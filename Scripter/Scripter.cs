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
            Script script = new Script("Example Script");
            script.Prompts.Add(new Prompt(PromptType.Text, "How are you?", PromptResponseType.Text));
            script.Prompts.Add(new Prompt(PromptType.Text, "How are you?", PromptResponseType.Voice));
            script.Prompts.Add(new Prompt(PromptType.Voice, "How are you?", PromptResponseType.Text));
            script.Prompts.Add(new Prompt(PromptType.Voice, "How are you?", PromptResponseType.Voice));
            script.Save(@"..\..\Scripts\ExampleScript.json");
        }
    }
}
