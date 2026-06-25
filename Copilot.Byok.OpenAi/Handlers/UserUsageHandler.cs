using Copilot.Byok.OpenAi.Models;
using Copilot.Byok.OpenAi.Services;

namespace Copilot.Byok.OpenAi.Handlers
{
    sealed class UserUsageHandler
    {
        public static UserUsage[] Get(UserUsageService requestStatService)
        {
            return requestStatService.GetRequestStats();
        }
    }
}
