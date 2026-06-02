package com.yourcompany.voiceplugin;

import android.app.Activity;
import android.content.Intent;
import android.speech.RecognizerIntent;
import com.unity3d.player.UnityPlayer;
import java.util.ArrayList;

public class VoicePlugin extends Activity {
    
    private static final int SPEECH_REQUEST = 100;
    
    public static void startVoiceRecognition() {
        Activity activity = UnityPlayer.currentActivity;
        Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
        intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL,
            RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
        intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE, "es-ES");
        intent.putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 3);
        activity.startActivityForResult(intent, SPEECH_REQUEST);
    }

    @Override
    protected void onActivityResult(int requestCode, 
        int resultCode, Intent data) {
        if (requestCode == SPEECH_REQUEST && 
            resultCode == Activity.RESULT_OK && data != null) {
            ArrayList<String> results = data.getStringArrayListExtra(
                RecognizerIntent.EXTRA_RESULTS);
            if (results != null && !results.isEmpty()) {
                // Enviar resultado a Unity
                UnityPlayer.UnitySendMessage(
                    "VoiceManager",      // Nombre del GameObject
                    "OnSpeechResult",    // Método a llamar
                    results.get(0)       // Texto reconocido
                );
            }
        } else {
            UnityPlayer.UnitySendMessage(
                "VoiceManager", 
                "OnSpeechError", 
                "No se reconoció nada"
            );
        }
    }
}