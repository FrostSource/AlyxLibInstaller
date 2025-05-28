using Source2HelperLibrary;

namespace AlyxLibInstallerForms;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        foreach (var addonName in HLA.GetAddonNames())
        {
            var t = new ToolStripMenuItem(addonName);
            addonsToolStripMenuItem.DropDownItems.Add(t);
        }
    }

    private void label1_Click(object sender, EventArgs e)
    {

    }

    private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
    {

    }
}
