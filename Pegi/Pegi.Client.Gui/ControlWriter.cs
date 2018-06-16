using System;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Pegi.Client.Gui
{
    public class ControlWriter : TextWriter
    {
        private readonly RichTextBox _rtb;

        public ControlWriter(RichTextBox rtb)
        {
            _rtb = rtb;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            _rtb.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (value == '\n')
                {
                    _rtb.AppendText("\n");
                    _rtb.ScrollToEnd();
                }
                else if (char.IsControl(value))
                {
                }
                else
                {
                    var tr = new TextRange(_rtb.Document.ContentEnd, _rtb.Document.ContentEnd)
                    {
                        Text = value.ToString(),
                    };
                    //tr.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Console.ForegroundColor.ToColor()));
                }
            }));
        }
    }
}
