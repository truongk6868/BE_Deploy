using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CondotelManagement.Helpers
{
    public static class ModelStateExtensions
    {
        public static Dictionary<string, string[]> ToErrorDictionary(this ModelStateDictionary modelState)
        {
            return modelState
                .Where(ms => ms.Value.Errors.Count > 0)
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
        }
    }
}
