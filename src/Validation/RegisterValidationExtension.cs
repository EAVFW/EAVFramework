using Microsoft.Extensions.DependencyInjection;

namespace EAVFramework.Validation
{
    public static class RegisterValidationExtension
    {
        public static void RegisterValidator<TValidation, TType>(this IServiceCollection serviceCollection) where TValidation : class
        {
            serviceCollection.AddSingleton<TValidation>();
            serviceCollection.AddSingleton<ValidatorMetaData>(new ValidatorMetaData<TType>
            {
                Handler = typeof(TValidation)
            });
        }
    }
}