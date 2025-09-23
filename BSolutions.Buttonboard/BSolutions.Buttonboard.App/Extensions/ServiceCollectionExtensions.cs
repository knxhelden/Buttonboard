using Microsoft.Extensions.DependencyInjection;
using System;

namespace BSolutions.Buttonboard.App.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> to register either a "real"
    /// or a "mock/simulated" implementation of an abstraction based on a runtime predicate.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <typeparamref name="TAbstraction"/> as a <see cref="ServiceLifetime.Singleton"/>
        /// and chooses between <typeparamref name="TReal"/> and <typeparamref name="TMock"/> at resolve time
        /// by evaluating <paramref name="useMockPredicate"/>.
        /// </summary>
        /// <typeparam name="TAbstraction">
        /// The service interface or base type to be registered.
        /// </typeparam>
        /// <typeparam name="TReal">
        /// The concrete "real" implementation used when the predicate returns <c>false</c>.
        /// Must implement <typeparamref name="TAbstraction"/>.
        /// </typeparamref>
        /// <typeparam name="TMock">
        /// The concrete "mock/simulated" implementation used when the predicate returns <c>true</c>.
        /// Must implement <typeparamref name="TAbstraction"/>.
        /// </typeparam>
        /// <param name="services">The DI service collection.</param>
        /// <param name="useMockPredicate">
        /// A function that receives the current <see cref="IServiceProvider"/> and returns
        /// <c>true</c> to use <typeparamref name="TMock"/> or <c>false</c> to use <typeparamref name="TReal"/>.
        /// Typical inputs are configuration flags (e.g. <c>OperationMode == Simulated</c>).
        /// </param>
        /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
        /// <remarks>
        /// <para>
        /// The registration uses a singleton factory:
        /// the predicate is evaluated once on the first resolution of <typeparamref name="TAbstraction"/>,
        /// then the chosen instance is created via <see cref="ActivatorUtilities.CreateInstance{T}(IServiceProvider, object[])"/>
        /// and reused for the application's lifetime.
        /// </para>
        /// <para>
        /// <b>Lifetime considerations:</b> Because this registers a singleton, avoid having
        /// <typeparamref name="TReal"/> or <typeparamref name="TMock"/> depend on <c>Scoped</c> services.
        /// If a scoped dependency is unavoidable, consider using a different lifetime or injecting an
        /// <c>IServiceScopeFactory</c> inside the implementation to create scopes as needed.
        /// </para>
        /// <para>
        /// <b>Disposal:</b> If the chosen implementation implements <see cref="IDisposable"/>,
        /// the host will dispose it when the container is disposed.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// services.AddByMode&lt;IOpenHabClient, OpenHabClient, OpenHabClientMock&gt;(sp =>
        /// {
        ///     var app = sp.GetRequiredService&lt;ISettingsProvider&gt;().Application;
        ///     return app.OperationMode == OperationMode.Simulated || app.TestOperation;
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddByMode<TAbstraction, TReal, TMock>(
            this IServiceCollection services,
            Func<IServiceProvider, bool> useMockPredicate)
            where TAbstraction : class
            where TReal : class, TAbstraction
            where TMock : class, TAbstraction
        {
            services.AddSingleton<TAbstraction>(sp =>
            {
                var useMock = useMockPredicate(sp);
                return useMock
                    ? ActivatorUtilities.CreateInstance<TMock>(sp)
                    : ActivatorUtilities.CreateInstance<TReal>(sp);
            });
            return services;
        }
    }
}