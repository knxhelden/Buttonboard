using System;

/// <summary>
/// Selects how the application interacts with external systems (hardware/services).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Real"/> — Use real integrations (GPIO, REST/MQTT backends, etc.).
/// </para>
/// <para>
/// <see cref="Simulated"/> — Use mock/in-memory implementations with no side effects,
/// suitable for local development, testing, or demos.
/// </para>
/// </remarks>
public enum OperationMode
{
    /// <summary>
    /// Run against real hardware and live services.
    /// </summary>
    Real,

    /// <summary>
    /// Run in simulation mode using mocks/fakes, without touching real systems.
    /// </summary>
    Simulated
}
