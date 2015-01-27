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
                    all.Prompts.Add(new Prompt(output, "How are you?", "How were you {0}?", input, 10000));
            all.Save(@"..\..\Scripts\ExampleAllInteractions.json");

            Script textOutputOnly = new Script("Text Output");
            textOutputOnly.Prompts.Add(new Prompt(PromptOutputType.Text, "Hi", null, PromptInputType.None, 0));
            textOutputOnly.Save(@"..\..\Scripts\ExampleTextOutput.json");

            Script textPrompt = new Script("Text Prompt");
            textPrompt.Prompts.Add(new Prompt(PromptOutputType.Text, "How are you?", "How were you {0}?", PromptInputType.Text, 10000));
            textPrompt.Save(@"..\..\Scripts\ExampleTextPrompt.json");

            Script longVoicePrompt = new Script("Long Voice Prompt");
            longVoicePrompt.Prompts.Add(new Prompt(PromptOutputType.Voice, "This is a long voice prompt. It should take at least a little while to complete. If it's not long enough, use the scripter to make it even longer.", null, PromptInputType.Voice, 20000, true, 3));
            longVoicePrompt.Save(@"..\..\Scripts\ExampleLongVoicePrompt.json");

            Script voiceOutputOnly = new Script("Voice Output");
            voiceOutputOnly.Prompts.Add(new Prompt(PromptOutputType.Voice, "Hi", null, PromptInputType.None, 0));
            voiceOutputOnly.Save(@"..\..\Scripts\ExampleVoiceOutput.json");

            Script voiceOutputOnlyDelayed = new Script("Voice Output Delayed", 5000);
            voiceOutputOnlyDelayed.Prompts.Add(new Prompt(PromptOutputType.Voice, "Hi", null, PromptInputType.None, 0));
            voiceOutputOnlyDelayed.Save(@"..\..\Scripts\ExampleVoiceOutputDelayed.json");

            Script voicePrompt = new Script("Voice Prompt");
            voicePrompt.Prompts.Add(new Prompt(PromptOutputType.Voice, "How are you?", "How were you {0}?", PromptInputType.Voice, 10000));
            voicePrompt.Save(@"..\..\Scripts\ExampleVoicePrompt.json");
        }
    }
}
