using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading;

namespace KuVoltUpdater
{
    class Logger
    {
        static TextBox _textBox;
        public static void SetLogBox(TextBox textBox)
        {
            _textBox = textBox;
        }
        public static void Write(string value)
        {
            _textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                _textBox.AppendText(value);
                _textBox.ScrollToEnd();
            }), DispatcherPriority.Loaded);
        }
        public static void Write(string value, params object[] args)
        {
            Write(string.Format(value), args);
        }
        public static void WriteLine(string value)
        {
            _textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                _textBox.AppendText(value);
                _textBox.AppendText(Environment.NewLine);
                _textBox.ScrollToEnd();
            }), DispatcherPriority.Loaded);
        }
        public static void WriteLine(string value, object args0)
        {
            WriteLine(string.Format(value, args0));
        }
        public static void WriteLine(string value, object args0, object args1)
        {
            WriteLine(string.Format(value, args0, args1));
        }
        public static void WriteLine(string value, object args0, object args1, object args2)
        {
            WriteLine(string.Format(value, args0, args1, args2));
        }
        public static void WriteLine(string value, params object[] args)
        {
            WriteLine(string.Format(value, args));
        }
        public static void WriteLine()
        {
            _textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                _textBox.AppendText(Environment.NewLine);
                _textBox.ScrollToEnd();
            }), DispatcherPriority.Loaded);
        }
    }
}
