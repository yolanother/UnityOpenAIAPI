using DoubTech.ThirdParty.OpenAI.Data;
using UnityEngine;

namespace DoubTech.ThirdParty.OpenAI.Scripts.Data
{
    [CreateAssetMenu(fileName = "OpenAIBasePrompt", menuName = "DoubTech/Third Party/OpenAI/Base Prompt", order = 0)]
    public class BasePrompt : ScriptableObject
    {
        [SerializeField] public Message[] messages;
    }
}