using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace JinlingKnot.ArtUI
{
    public sealed class MenuInteractions : MonoBehaviour
    {
        public Button start, demo, settings, sound, fullscreen, exit, closeSettings;
        public GameObject settingsPanel;
        public Slider volume;
        public UnityEvent onStart = new UnityEvent(), onDemo = new UnityEvent(), onExit = new UnityEvent();
        bool muted;
        float rememberedVolume = 1;
        void Awake()
        {
            start.onClick.AddListener(()=>onStart.Invoke());
            demo.onClick.AddListener(()=>onDemo.Invoke());
            exit.onClick.AddListener(()=>onExit.Invoke());
            settings.onClick.AddListener(()=>settingsPanel.SetActive(true));
            closeSettings.onClick.AddListener(()=>settingsPanel.SetActive(false));
            sound.onClick.AddListener(()=> { muted=!muted; AudioListener.volume=muted?0:rememberedVolume; sound.GetComponent<Image>().color=muted?new Color(.65f,.72f,.65f):Color.white; });
            fullscreen.onClick.AddListener(()=>Screen.fullScreen=!Screen.fullScreen);
            volume.value=AudioListener.volume;
            volume.onValueChanged.AddListener(v=>{rememberedVolume=v; if(!muted) AudioListener.volume=v;});
        }
        // Escape is handled by KnotUI so closing settings uses the same black transition.
    }
}
