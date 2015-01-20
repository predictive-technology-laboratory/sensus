using SensusService.Probes.User;
using System;
using System.Windows.Forms;

namespace Scripter
{
    public partial class Scripter : Form
    {
        public Scripter()
        {
            InitializeComponent();
        }

        private void Scripter_Load(object sender, EventArgs e)
        {
            Script all = new Script("All Interactions");
            foreach (PromptOutputType output in Enum.GetValues(typeof(PromptOutputType)))
                foreach (PromptInputType input in Enum.GetValues(typeof(PromptInputType)))
                    all.Prompts.Add(new Prompt(output, "How are you?", input));
            all.Save(@"..\..\Scripts\ExampleAllInteractions.json");

            Script voice = new Script("Voice Prompt");
            voice.Prompts.Add(new Prompt(PromptOutputType.Voice, "How are you?", PromptInputType.Voice));
            voice.Save(@"..\..\Scripts\ExampleVoicePrompt.json");

            Script text = new Script("Text Prompt");
            text.Prompts.Add(new Prompt(PromptOutputType.Text, "How are you?", PromptInputType.Text));
            text.Save(@"..\..\Scripts\ExampleTextPrompt.json");

            Script longVoice = new Script("Long Voice Prompt");
            longVoice.Prompts.Add(new Prompt(PromptOutputType.Voice, "This is a long voice prompt. It should take at least a little while to complete.", PromptInputType.Voice, true, 3));
            longVoice.Save(@"..\..\Scripts\ExampleLongVoicePrompt.json");

            Script voiceOutputOnly = new Script("Voice Output");
            voiceOutputOnly.Prompts.Add(new Prompt(PromptOutputType.Voice, "Hi", PromptInputType.None));
            voiceOutputOnly.Save(@"..\..\Scripts\ExampleVoiceOutput.json");

            Script voiceOutputDelayed = new Script("Voice Output Delayed", 5000);
            voiceOutputDelayed.Prompts.Add(new Prompt(PromptOutputType.Voice, "Hi", PromptInputType.None));
            voiceOutputDelayed.Save(@"..\..\Scripts\ExampleVoiceOutputDelayed.json");
        }
    }
}
