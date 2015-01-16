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
            foreach (PromptType output in Enum.GetValues(typeof(PromptType)))
                foreach (PromptResponseType input in Enum.GetValues(typeof(PromptResponseType)))
                    all.Prompts.Add(new Prompt(output, "How are you?", input));
            all.Save(@"..\..\Scripts\ExampleScriptAllInteractions.json");

            Script voice = new Script("Voice Prompt");
            voice.Prompts.Add(new Prompt(PromptType.Voice, "How are you?", PromptResponseType.Voice));
            voice.Save(@"..\..\Scripts\ExampleVoicePrompt.json");

            Script text = new Script("Text Prompt");
            text.Prompts.Add(new Prompt(PromptType.Text, "How are you?", PromptResponseType.Text));
            text.Save(@"..\..\Scripts\ExampleTextPrompt.json");

            Script voiceOutputOnly = new Script("Voice Output");
            voiceOutputOnly.Prompts.Add(new Prompt(PromptType.Voice, "Hi", PromptResponseType.None));
            voiceOutputOnly.Save(@"..\..\Scripts\ExampleVoiceOutput.json");
        }
    }
}
