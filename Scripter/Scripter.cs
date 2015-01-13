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
            Prompt example = new Prompt(PromptType.Voice, "How are you?", PromptResponseType.Voice);
            Script script = new Script("Example Script", example);
            script.Save(@".\Scripts\test.json");
        }
    }
}
