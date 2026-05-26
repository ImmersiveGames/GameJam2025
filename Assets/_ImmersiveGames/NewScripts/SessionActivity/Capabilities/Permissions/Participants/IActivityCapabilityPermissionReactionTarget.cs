/// <summary>
/// Contrato de reação local para capabilities/endpoints que respondem à permission já resolvida.
/// Não declara metadata de participant e não decide lifecycle macro.
/// </summary>
public interface IActivityCapabilityPermissionReactionTarget
{
    void OnPermissionAllowed(ActivityCapabilityPermissionParticipantContext context);
    void OnPermissionBlocked(ActivityCapabilityPermissionParticipantContext context);
    void OnPermissionUnbound(ActivityCapabilityPermissionParticipantContext context);
}
