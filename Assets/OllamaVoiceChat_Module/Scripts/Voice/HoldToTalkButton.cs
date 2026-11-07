using UnityEngine;
using UnityEngine.EventSystems;

namespace OllamaVoice
{
    /// <summary>
    /// Botón de micrófono push-to-talk: mantener presionado para hablar,
    /// soltar para enviar. Salir del botón cancela sin enviar.
    /// </summary>
    public class HoldToTalkButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private VoiceChatController voice;
        private bool holding;

        private void Awake()
        {
            if (voice == null)
            {
                voice = FindAnyObjectByType<VoiceChatController>(FindObjectsInactive.Include);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            holding = true;
            if (voice != null) voice.StartHoldRecording();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!holding) return;
            holding = false;
            if (voice != null) voice.ReleaseHoldAndSend();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!holding) return;
            holding = false;
            if (voice != null) voice.CancelHold();
        }

        public void Setup(VoiceChatController controller)
        {
            voice = controller;
        }
    }
}
