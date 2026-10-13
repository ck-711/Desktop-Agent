# uLipSync Evaluation

Source: https://github.com/hecomi/uLipSync

## Fit

uLipSync is a suitable lightweight runtime viseme analyzer for this project. It
can analyze the active TTS AudioSource and produce the five Japanese vowel
classes used by VRM 1.0: A, I, U, E, and O. Those values map directly to
`ExpressionKey.Aa`, `ExpressionKey.Ih`, `ExpressionKey.Ou`,
`ExpressionKey.Ee`, and `ExpressionKey.Oh`.

## Constraint

It is profile-based. A profile trained or calibrated using audio from the
actual Qwen3 CustomVoice is required for useful Chinese output. A generic or
Japanese profile will animate the mouth, but its vowel classification for
Mandarin finals and the umlaut vowel is not reliable.

## Recommended Integration

Attach uLipSync to the same GameObject as the TTS AudioSource. Feed its five
output weights to a small VRM 1.0 bridge which calls
`vrm.Runtime.Expression.SetWeight` for the five VRM expression keys. The
existing TTS synthesis and playback queues do not need to change.

Use a calibration set containing sustained A, I, U, E, O recordings made with
the selected Qwen3 voice before enabling the bridge. Treat the umlaut vowel as
a tuned blend of I and U if the profile does not classify it satisfactorily.
