using UnityEngine;
public class FootstepForwarder : MonoBehaviour
{
    public FCG.CharacterControlHybrid mainScript; // Kéo First Person Player vào đây
    private void OnFootstep(AnimationEvent animationEvent)
    {
        mainScript.SendMessage("OnFootstep", animationEvent);
    }
    private void OnLand(AnimationEvent animationEvent)
    {
        mainScript.SendMessage("OnLand", animationEvent);
    }
}