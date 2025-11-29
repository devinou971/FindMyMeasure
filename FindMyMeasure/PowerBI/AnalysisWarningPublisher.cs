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
        private List<IMissingMeasureWarningSubscriber> _missingMeasureSubscribers = new List<IMissingMeasureWarningSubscriber>();
        private List<IMissingColumnWarningSubscriber> _missingColumnSubscribers = new List<IMissingColumnWarningSubscriber>();

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

        public void PublishWarning(AnalysisWarning warning)
        {
            _warnings.Add(warning);
            foreach (var subscriber in _subscribers)
            {
                subscriber.OnWarningReceived(warning);
            }
        }

        public void PublishWarning(MissingMeasureWarning warning)
        {
            PublishWarning((AnalysisWarning)warning);
            foreach (var subscriber in _missingMeasureSubscribers)
            {
                subscriber.OnWarningReceived(warning);
            }
        }

        public void PublishWarning(MissingColumnWarning warning)
        {
            PublishWarning((AnalysisWarning)warning);
            foreach (var subscriber in _missingColumnSubscribers)
            {
                subscriber.OnWarningReceived(warning);
            }
        }
    }
}
