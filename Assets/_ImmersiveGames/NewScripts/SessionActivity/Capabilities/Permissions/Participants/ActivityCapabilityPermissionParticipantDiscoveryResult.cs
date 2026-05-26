using System;
using System.Collections.Generic;

/// <summary>
/// Resultado da descoberta determinística de participantes locais de permission.
/// </summary>
public sealed class ActivityCapabilityPermissionParticipantDiscoveryResult
{
    public ActivityCapabilityPermissionParticipantDiscoveryResult(
        IReadOnlyList<ActivityCapabilityPermissionParticipantDescriptor> participants,
        IReadOnlyList<string> warnings)
    {
        Participants = participants ?? Array.Empty<ActivityCapabilityPermissionParticipantDescriptor>();
        Warnings = warnings ?? Array.Empty<string>();
    }

    public IReadOnlyList<ActivityCapabilityPermissionParticipantDescriptor> Participants { get; }
    public IReadOnlyList<string> Warnings { get; }

    public int ParticipantCount => Participants.Count;
    public int WarningCount => Warnings.Count;
    public bool HasParticipants => ParticipantCount > 0;
    public bool HasWarnings => WarningCount > 0;
}
