using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mutia.FileMQ
{
    public class FilePublisher<TMessage> : IDisposable, IPublisher<TMessage> where TMessage : class
    {
        private readonly string _directory;
        private readonly List<ISubscriber<TMessage>> subscribers = new List<ISubscriber<TMessage>>();
        private readonly string _messageDirectory;
        private readonly string _publisherDirectory;
        private readonly string _objectName;
        private bool IsStopping = false;
        private ConcurrentQueue<TMessage> _messages = new ConcurrentQueue<TMessage>();
        private readonly Guid _id;
        private readonly DateTime _creationTime;
        private ActivePublishers ActivePublishers = new ActivePublishers();
        private List<ProcessedFile> _processedFiles = new List<ProcessedFile>();
        private readonly FileBusOptions _fileBusOptions = new FileBusOptions();

        public FilePublisher(string directory, Guid id, FileBusOptions fileBusOptions = null)
        {
            _directory = directory;
            _id = id;
            _creationTime = DateTime.UtcNow;

            if (fileBusOptions != null)
            {
                _fileBusOptions = fileBusOptions;
            }

            var messageType = typeof(TMessage);
            _objectName = messageType.FullName;
            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }

            _messageDirectory = Path.Combine(_directory, messageType.FullName);
            if (!Directory.Exists(_messageDirectory))
            {
                Directory.CreateDirectory(_messageDirectory);
            }

            _publisherDirectory = Path.Combine(_messageDirectory, _id.ToString());
            if (!Directory.Exists(_publisherDirectory))
            {
                Directory.CreateDirectory(_publisherDirectory);
            }

            _ = StartPublisherHeartbeat();
            _ = StartFilesWatcher();
        }


        private async Task StartPublisherHeartbeat()
        {

            while (!IsStopping)
            {
                try
                {
                    var publishersListFile = Path.Combine(_messageDirectory, "publishers.json");

                    using (var fileStream = File.Open(publishersListFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    {
                        var filesJson = string.Empty;
                        var me = new PublisherRegistry(_id, _creationTime);

                        var reader = new StreamReader(fileStream);
                        filesJson = reader.ReadToEnd();

                        List<PublisherRegistry> updatedPublishers = new List<PublisherRegistry>();

                        if (!string.IsNullOrWhiteSpace(filesJson))
                        {
                            var existingPublishers = JsonSerializer.Deserialize<List<PublisherRegistry>>(filesJson);

                            if (existingPublishers != null)
                            {
                                existingPublishers.RemoveAll(x => x.PublisherId == _id);

                                updatedPublishers.AddRange(existingPublishers);
                            }

                            updatedPublishers.Add(me);
                        }

                        using (var fw = new StreamWriter(fileStream))
                        {
                            fileStream.Position = 0;
                            fw.Write(JsonSerializer.Serialize(updatedPublishers));
                            fileStream.SetLength(fw.BaseStream.Position);
                        }

                        reader.Close();
                    }
                }
                catch (Exception)
                {

                }
                finally
                {
                    await Task.Delay(_fileBusOptions.PublisherHeartBeatDelay);
                }
            }
        }

        private async Task StartFilesWatcher()
        {
            while (!IsStopping)
            {
                try
                {
                    var files = Directory.GetFiles(_publisherDirectory, $"{_objectName}*.json");

                    foreach (var file in files)
                    {
                        await ProcessFile(file);
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    await Task.Delay(_fileBusOptions.FilesWatcherRefreshDelay);
                }
            }

        }

        private async Task ProcessFile(string file)
        {
            int retry = 0;
        processfile:
            try
            {
                var message = JsonSerializer.Deserialize<TMessage>(File.ReadAllText(file));

                await InternalPublish(message);

                int deleteFileRetry = 0;
            deletefile:
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception)
                {
                    if (deleteFileRetry < _fileBusOptions.MaxRetry && !IsStopping)
                    {
                        ++deleteFileRetry;
                        goto deletefile;
                    }
                }

            }
            catch (Exception)
            {
                if (retry < _fileBusOptions.MaxRetry && !IsStopping)
                {
                    ++retry;
                    goto processfile;
                }
            }
        }

        private Task InternalPublish(TMessage message)
        {
            if (message != null)
            {
                foreach (var subscriber in subscribers)
                {
                    try
                    {
                        _ = subscriber.HandleMessage(message);
                    }
                    catch
                    {
                    }
                }
            }

            return Task.CompletedTask;
        }

        public Task Publish(TMessage message)
        {
            int retry = 0;
        publish:
            try
            {
                var publishers = ActivePublishers.Publishers;

                if (!ActivePublishers.LastRefresh.HasValue || ActivePublishers.LastRefresh < DateTime.UtcNow.AddMilliseconds(-_fileBusOptions.ActivePublishersRefreshDelay))
                {
                    var publishersFile = Path.Combine(_messageDirectory, "publishers.json");

                    if (File.Exists(publishersFile))
                    {
                        var publishersJson = File.ReadAllText(publishersFile);

                        publishers = JsonSerializer.Deserialize<List<PublisherRegistry>>(publishersJson);
                    }

                    ActivePublishers.Publishers = publishers;
                    ActivePublishers.LastRefresh = DateTime.UtcNow;
                }


                foreach (var publsher in publishers)
                {
                    try
                    {
                        _ = Task.Run(() =>
                        {
                            PublisheToSinglePublisher(publsher.PublisherId, message);
                        });
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            catch (IOException)
            {
                if (retry < _fileBusOptions.MaxRetry && !IsStopping)
                {
                    ++retry;
                    goto publish;
                }
            }

            catch (Exception)
            {

            }

            return Task.CompletedTask;
        }

        private void PublisheToSinglePublisher(Guid id, TMessage message)
        {
            // retry in case of failure
            int retry = 0;
        retrypublishing:
            try
            {
                var data = JsonSerializer.Serialize(message);
                var messageFileName = Path.Combine(_messageDirectory, id.ToString(), $"{_objectName}{DateTime.Now:yyyyMMddHHmmssfff}{Guid.NewGuid()}.json");
                File.WriteAllText(messageFileName, data);
            }
            catch (Exception)
            {
                if (retry < _fileBusOptions.MaxRetry && !IsStopping)
                {
                    ++retry;
                    goto retrypublishing;
                }
            }
        }

        public Task Subscribe(ISubscriber<TMessage> subscriber)
        {
            if (!subscribers.Contains(subscriber))
                subscribers.Add(subscriber);

            return Task.CompletedTask;
        }

        public Task Unsubscribe(ISubscriber<TMessage> subscriber)
        {
            if (subscribers.Contains(subscriber))
                subscribers.Remove(subscriber);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            IsStopping = true;
        }
    }
}
