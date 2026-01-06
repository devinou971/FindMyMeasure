using FindMyMeasure.Interfaces;
using FindMyMeasure.WarningClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindMyMeasure.PowerBI
{
    public class AnalysisWarningPublisher
    {
        private static AnalysisWarningPublisher _instance = null;

        private List<AnalysisWarning> _warnings = new List<AnalysisWarning>();

        private List<IAnalysisWarningSubscriber> _subscribers = new List<IAnalysisWarningSubscriber>();
        private List<IMissingArtifactWarningSubscriber> _missingArtifactSubscribers = new List<IMissingArtifactWarningSubscriber>();
        private List<IMissingMeasureWarningSubscriber> _missingMeasureSubscribers = new List<IMissingMeasureWarningSubscriber>();
        private List<IMissingColumnWarningSubscriber> _missingColumnSubscribers = new List<IMissingColumnWarningSubscriber>();
        private List<IMissingHierarchyWarningSubscriber> _missingHierarchySubscribers = new List<IMissingHierarchyWarningSubscriber>();

        private AnalysisWarningPublisher() {}

        public static AnalysisWarningPublisher GetInstance()
        {
            if (_instance == null)
                _instance = new AnalysisWarningPublisher();
            return _instance;
        }

        public void Subscribe(IAnalysisWarningSubscriber subscriber)
        {
            if (!_subscribers.Contains(subscriber))
                _subscribers.Add(subscriber);
        }

        public void SubscribeToMissingArtifactWarning(IMissingArtifactWarningSubscriber subscriber)
        {
            if (!_missingArtifactSubscribers.Contains(subscriber))
                _missingArtifactSubscribers.Add(subscriber);
        }

        public void SubscribeToMissingMeasureWarning(IMissingMeasureWarningSubscriber subscriber)
        {
            if (!_missingMeasureSubscribers.Contains(subscriber))
                _missingMeasureSubscribers.Add(subscriber);
        }

        public void SubscribeToMissingColumnWarning(IMissingColumnWarningSubscriber subscriber)
        {
            if (!_missingColumnSubscribers.Contains(subscriber))
                _missingColumnSubscribers.Add(subscriber);
        }

        public void SubscribeToMissingHierarchyWarning(IMissingHierarchyWarningSubscriber subscriber)
        {
            if (!_missingHierarchySubscribers.Contains(subscriber))
                _missingHierarchySubscribers.Add(subscriber);
        }

        public void PublishWarning(AnalysisWarning warning)
        {
            _warnings.Add(warning);
            foreach (var subscriber in _subscribers)
            {
                subscriber.OnWarningReceived(warning);
            }
        }

        public void PublishWarning(MissingArtifactWarning warning)
        {
            PublishWarning((AnalysisWarning)warning);
            foreach (var subscriber in _missingArtifactSubscribers)
            {
                subscriber.OnWarningReceived(warning);
            }
            switch(warning.ArtifactType)
            {
                case "Column":
                    foreach (var subscriber in _missingColumnSubscribers)
                        subscriber.OnWarningReceived(new MissingColumnWarning(warning.Sender, warning.ArtifactName, warning.TableName));
                    break;
                case "Measure":
                    foreach (var subscriber in _missingMeasureSubscribers)
                        subscriber.OnWarningReceived(new MissingMeasureWarning(warning.Sender, warning.ArtifactName, warning.TableName));
                    break;
                case "Hierarchy":
                    foreach (var subscriber in _missingHierarchySubscribers)
                        subscriber.OnWarningReceived(new MissingHierarchyWarning(warning.Sender, warning.ArtifactName, warning.TableName));
                    break;
            }
        }
    }
}
