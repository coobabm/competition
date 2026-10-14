import unittest
import tempfile
import struct
from pathlib import Path
import wave
import voiceover

class VoiceoverTests(unittest.TestCase):
    def test_key_matches_unity(self):
        self.assertEqual(voiceover.key('<b>……你好，42！</b>'), '你好42')
    def test_pauses_do_not_change_words(self):
        raw = '……它在学。真的？'
        marked = voiceover.direct(raw, 'reflective')
        self.assertIn('[pause ', marked)
        self.assertEqual(voiceover.key(voiceover.PAUSE.sub('', marked)), voiceover.key(raw))
    def test_excludes_empty_monologue(self):
        self.assertEqual(voiceover.key('……'), '')
    def test_ids_are_deterministic(self):
        self.assertEqual(voiceover.clip_id('你好。'), voiceover.clip_id('你好！'))
    def test_feeding_data_is_not_an_alarm_interjection(self):
        self.assertEqual(voiceover.performance('也没换什么。就是喂的东西够多了。', '')[0], 'reflective')

    def test_dynamic_is_explicit(self):
        self.assertTrue(voiceover.is_dynamic('现在是{time}'))
        self.assertFalse(voiceover.is_dynamic('它在学。'))

    def test_inventory_excludes_internal_flag_names(self):
        self.assertFalse(any(row['text'] == 'era.context.choice' for row in voiceover.inventory()['entries']))

    def test_broken_checkpoint_is_treated_as_unfinished(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'proof.json'
            path.write_text('{')
            self.assertEqual(voiceover.read_proof(path), {})

    def test_truncated_wav_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'cut.wav'
            with wave.open(str(path), 'wb') as out:
                out.setparams((1, 2, 24000, 0, 'NONE', 'not compressed'))
                out.writeframes(struct.pack('<h', 4000) * 48000)
            path.write_bytes(path.read_bytes()[:44 + 9600])
            with self.assertRaises(ValueError): voiceover.stats(path)

if __name__ == '__main__': unittest.main()
