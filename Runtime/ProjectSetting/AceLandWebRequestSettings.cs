using System;
using AceLand.Library.BuildLeveling;
using AceLand.Library.ProjectSetting;
using UnityEngine;

namespace AceLand.WebRequest.ProjectSetting
{
    public class AceLandWebRequestSettings : ProjectSettings<AceLandWebRequestSettings>
    {
        [SerializeField] private BuildLevel loggingLevel = BuildLevel.Production;
        [SerializeField] private BuildLevel resultLoggingLevel = BuildLevel.Development;
        [SerializeField] private bool checkJsonBeforeSend;
        [SerializeField] private bool forceHttpsScheme;
        [SerializeField] private bool addTimeInHeader;
        [SerializeField] private string timeKey = "Time";
        [SerializeField, Min(0)] private int requestTimeout = 3000;
        [SerializeField, Min(0)] private int longRequestTimeout = 15000;
        [SerializeField] private int requestRetry = 3;
        [SerializeField] private int[] retryInterval = { 400, 800, 1600, 3200, 6400, 12800, 25600 };

        public BuildLevel LoggingLevel => loggingLevel;
        public BuildLevel ResultLoggingLevel => resultLoggingLevel;
        public bool CheckJsonBeforeSend => checkJsonBeforeSend;
        public bool ForceHttpsScheme => forceHttpsScheme;
        public bool AddTimeInHeader => addTimeInHeader;
        public string TimeKey => timeKey;
        public int RequestTimeout => requestTimeout;
        public int LongRequestTimeout => longRequestTimeout;
        public int RequestRetry => requestRetry;
        public ReadOnlySpan<int> RetryInterval => retryInterval;

        public int GetRetryInterval(int retry)
        {
            return retry <= Mathf.Min(requestRetry, retryInterval.Length)
                ? retryInterval[retry - 1]
                : -1;
        }
    }
}