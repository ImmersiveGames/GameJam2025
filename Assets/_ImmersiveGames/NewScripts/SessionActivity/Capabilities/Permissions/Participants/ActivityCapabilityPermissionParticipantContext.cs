using System;

/// <summary>
/// Contexto determinístico entregue ao participante local quando uma permission muda de estado.
/// O contexto carrega a identidade da Activity Entry para que callbacks stale/foreign possam ser rejeitados
/// por quem integra este contrato ao runtime.
/// </summary>
public readonly struct ActivityCapabilityPermissionParticipantContext
{
    public ActivityCapabilityPermissionParticipantContext(
        string pipelineId,
        string sessionStateId,
        string activityId,
        int entrySequence,
        string permissionId,
        string targetId,
        string source,
        string reason)
    {
        PipelineId = pipelineId ?? string.Empty;
        SessionStateId = sessionStateId ?? string.Empty;
        ActivityId = activityId ?? string.Empty;
        EntrySequence = entrySequence;
        PermissionId = permissionId ?? string.Empty;
        TargetId = targetId ?? string.Empty;
        Source = source ?? string.Empty;
        Reason = reason ?? string.Empty;
    }

    public string PipelineId { get; }
    public string SessionStateId { get; }
    public string ActivityId { get; }
    public int EntrySequence { get; }
    public string PermissionId { get; }
    public string TargetId { get; }
    public string Source { get; }
    public string Reason { get; }

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(PipelineId) &&
        !string.IsNullOrWhiteSpace(SessionStateId) &&
        !string.IsNullOrWhiteSpace(ActivityId) &&
        EntrySequence > 0 &&
        !string.IsNullOrWhiteSpace(PermissionId);

    public override string ToString()
    {
        return $"pipelineId='{PipelineId}', sessionStateId='{SessionStateId}', activityId='{ActivityId}', entrySequence='{EntrySequence}', permissionId='{PermissionId}', targetId='{TargetId}', source='{Source}', reason='{Reason}'";
    }
}
