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
            Script script1 = new Script("All Interactions");
            script1.Prompts.Add(new Prompt(PromptType.Text, "How are you?", PromptResponseType.Text));
            script1.Prompts.Add(new Prompt(PromptType.Text, "How are you?", PromptResponseType.Voice));
            script1.Prompts.Add(new Prompt(PromptType.Voice, "How are you?", PromptResponseType.Text));
            script1.Prompts.Add(new Prompt(PromptType.Voice, "How are you?", PromptResponseType.Voice));
            script1.Save(@"..\..\Scripts\ExampleScriptAllInteractions.json");

            Script script2 = new Script("Output Only");
            script2.Prompts.Add(new Prompt(PromptType.Text, "Fired", PromptResponseType.None));
            script2.Prompts.Add(new Prompt(PromptType.Voice, "Fired", PromptResponseType.None));
            script2.Save(@"..\..\Scripts\ExampleScriptOutputOnly.json");
        }
    }
}
