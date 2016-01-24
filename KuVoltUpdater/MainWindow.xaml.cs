using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using System.Xml;

namespace KuVoltUpdater
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        AssemblyName assemblyName;
        Updater updater;
        string origin;

        public MainWindow()
        {
            InitializeComponent();
            assemblyName = Assembly.GetExecutingAssembly().GetName();
            this.Title += string.Format(" v{0}.{1}", assemblyName.Version.Major, assemblyName.Version.Minor);
            Logger.SetLogBox(this.logBox);
            Logger.WriteLine(this.Title);
            Logger.WriteLine("개발자 : SteamB23@gmail.com");
            Logger.WriteLine();
            ImageSetting();
            Logger.WriteLine();
            try
            {
                LoadInfo();
                Logger.WriteLine();
                this.updater = new Updater(this, origin);
                updater.IntegrityCheck();
            }
            catch (System.IO.FileNotFoundException)
            {
                Logger.WriteLine("====경고 : xml 파일이 없습니다.");
                UpdateButtonError();
            }
            catch (XmlException)
            {
                Logger.WriteLine("====경고 : xml 파일을 읽을 수 없습니다.");
                UpdateButtonError();
            }
            catch (UriFormatException)
            {
                Logger.WriteLine("====경고 : 원본 위치가 잘못 되었습니다.");
                UpdateButtonError();
            }
        }
        void ImageSetting()
        {
            BitmapImage image = null;
            Uri imageUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + assemblyName.Name + ".png");
            try
            {
                image = new BitmapImage(imageUri);
            }
            catch (System.IO.FileNotFoundException)
            {
                imageUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + assemblyName.Name + ".jpg");
                try
                {
                    image = new BitmapImage(imageUri);
                }
                catch (System.IO.FileNotFoundException)
                {
                    imageUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + assemblyName.Name + ".gif");
                    try
                    {
                        image = new BitmapImage(imageUri);
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        imageUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + assemblyName.Name + ".bmp");
                        try
                        {
                            image = new BitmapImage(imageUri);
                        }
                        catch (System.IO.FileNotFoundException)
                        {
                            Logger.WriteLine("====이미지가 없습니다.");
                        }
                    }
                }
            }
            if (image != null)
            {
                this.titleImage.Source = image;
                Logger.WriteLine("====이미지 로딩 완료...");
            }
        }
        void LoadInfo()
        {
            XmlDocument xml = new XmlDocument();
            string xmlUri = string.Format("{0}.xml", assemblyName.Name);
            xml.Load(xmlUri);
            Logger.WriteLine("====xml 로딩 완료...");

            XmlNode nameNode = xml.SelectSingleNode("KuVolt/Name");
            Logger.WriteLine("프로젝트 이름 : {0}", nameNode.InnerText);
            this.Title += " - " + nameNode.InnerText;

            XmlNode originNode = xml.SelectSingleNode("KuVolt/Origin");
            this.origin = originNode.InnerText;
            Logger.WriteLine("원본 경로 : {0}", origin);
        }
        public void UpdateButtonError()
        {
            updateButton.Dispatcher.Invoke(() =>
            {
                updateButton.Content = "오류!";
            });
        }
        private void updateButton_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                this.updater.ForcedUpdate();
            }
            else
            {
                this.updater.Update();
            }
        }

        private void quitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

    }
}
