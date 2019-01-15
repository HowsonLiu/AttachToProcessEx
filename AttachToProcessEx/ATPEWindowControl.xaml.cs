namespace AttachToProcessEx
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    //using System.Windows.Forms;

    /// <summary>
    /// Interaction logic for ATPEWindowControl.
    /// </summary>
    public partial class ATPEWindowControl : UserControl
    {
        private ProcessInfoModel model;
        private VSAttacher attacher;

        /// <summary>
        /// Initializes a new instance of the <see cref="ATPEWindowControl"/> class.
        /// </summary>
        public ATPEWindowControl()
        {
            this.InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            this.model = new ProcessInfoModel();
            this.attacher = new VSAttacher();
            this.listView.ItemsSource = model.Processinfolist;
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
                "ATPEWindow");
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            this.model.UpdateProcessInfoList(this.textBox.Text);
        }

        private void Attach_Click(object sender, RoutedEventArgs e)
        {
            if (this.listView.SelectedItems.Count <= 0)
                return;
            ProcessInfo curinfo = this.listView.SelectedItem as ProcessInfo;
            attacher.Attach(curinfo.Pid);
        }
    }
}