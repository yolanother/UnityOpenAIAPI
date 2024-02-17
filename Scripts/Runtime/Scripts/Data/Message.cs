using System;
using UnityEngine;

namespace DoubTech.ThirdParty.OpenAI.Data
{
    [Serializable]
    public class Message
    {
        [Popup("user", "assistant", "system")]
        [SerializeField] public string role;
        [TextArea]
        [SerializeField] public string content;
    }
}