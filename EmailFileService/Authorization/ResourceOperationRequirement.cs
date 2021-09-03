using Microsoft.AspNetCore.Authorization;

namespace EmailFileService.Authorization
{
    public enum ResourceOperation
    {
        Access
    }
    public class ResourceOperationRequirement : IAuthorizationRequirement
    {

        public ResourceOperationRequirement(ResourceOperation resourceOperation)
        {
            ResourceOperation = resourceOperation;
        }

        public ResourceOperation ResourceOperation { get; }

    }
}
