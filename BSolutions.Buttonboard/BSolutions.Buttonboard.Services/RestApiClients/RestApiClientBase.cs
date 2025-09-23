using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace BSolutions.Buttonboard.Services.RestApiClients
{
    public abstract class RestApiClientBase : IDisposable
    {
        protected readonly ILogger<RestApiClientBase> _logger;
        protected readonly ISettingsProvider _settings;
        protected readonly HttpClient _httpClient;

        private bool disposed = false;

        #region --- Constructor ---

        public RestApiClientBase(ILogger<RestApiClientBase> logger, ISettingsProvider settingsProvider)
        {
            this._logger = logger;
            this._settings = settingsProvider; ;

            // HTTP Client
            this._httpClient = new HttpClient();
        }

        ~RestApiClientBase()
        {
            Dispose(false);
        }

        #endregion

        #region --- IDisposable ---

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                // if this is a dispose call dispose on all state you
                // hold, and take yourself off the Finalization queue.
                if (disposing)
                {
                    if (this._httpClient != null)
                    {
                        this._httpClient.Dispose();
                    }
                }

                this.disposed = true;
            }
        }

        #endregion
    }
}
