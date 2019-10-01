using System.Net.Http;
using System.Threading.Tasks;
using A19.Messaging.Rest;
using A19.System.Common;
using Mrh.Monad;

namespace A19.System.Rest
{
    /// <summary>
    ///     The rest client for connecting to a system.
    /// </summary>
    public class SystemRestClient : ISystemRestClient
    {

        private readonly IRestClient _restClient;

        public SystemRestClient(
            ISystemClientSettings systemClientSettings)
        {
            _restClient = new RestSystemClient(systemClientSettings.ResultUrl);
        }

        public async Task<IResultMonad<SystemLoginRs>> Login(SystemLoginRq systemLoginRq)
        {
            return await _restClient.PostAsync<SystemLoginRq, SystemLoginRs>(
                "System",
                "login",
                systemLoginRq);
        }

        public async Task<IResultMonad<ExtendSystemSessionRs>> Extend(ExtendSystemSessionRq request)
        {
            return await _restClient.PostAsync<ExtendSystemSessionRq, ExtendSystemSessionRs>(
                "System",
                "extend-session",
                request);
        }
    }
}