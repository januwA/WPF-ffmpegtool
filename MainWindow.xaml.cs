using System;
using fpath = System.IO.Path;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace WpfApp1
{
  /// <summary>
  /// MainWindow.xaml 的交互逻辑
  /// </summary>
  public partial class MainWindow : Window
  {
    public string ffmpegExe;
    public string ffplayExe;
    public string srcFilePath = "";
    public MainWindow()
    {
      InitializeComponent();
      this.AllowDrop = true;
      ffmpegExe = AppDomain.CurrentDomain.BaseDirectory + "ffmpeg.exe";
      ffplayExe = AppDomain.CurrentDomain.BaseDirectory + "ffplay.exe";

      if(System.IO.File.Exists(ffmpegExe) == false)
      {
        MessageBox.Show("未找到[ffmpeg.exe]", "文件未找到");
      }

      if (System.IO.File.Exists(ffplayExe) == false)
      {
        MessageBox.Show("未找到[ffplay.exe]", "文件未找到");
      }
    }

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
      if (e.Data.GetDataPresent(DataFormats.FileDrop))
        e.Effects = DragDropEffects.Copy;
      else
        e.Effects = DragDropEffects.None;

      e.Handled = true;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
      if (e.Data.GetDataPresent(DataFormats.FileDrop))
        e.Effects = DragDropEffects.Copy;
      else
        e.Effects = DragDropEffects.None;

      e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
      string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
      if (filenames.Length > 0)
      {
        srcFilePath = filenames[0];
        selectRes.Text = srcFilePath;
      }
    }

    /// <summary>
    /// 带控制台，方便看到更多信息,和错误
    /// </summary>
    /// <param name="command"></param>
    private void execute(string command)
    {
      try
      {
        System.Diagnostics.Process.Start(ffmpegExe, command);
      }
      catch (Exception)
      {
        MessageBox.Show("执行错误: " + command);
      }
    }


    /// <summary>
    /// 选择文件视频
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
      var openFileDialog = new Microsoft.Win32.OpenFileDialog()
      {
        Filter = "(*.*)|*.*"
      };
      if (openFileDialog.ShowDialog() == false) return;
      srcFilePath = openFileDialog.FileName;
      selectRes.Text = srcFilePath;
    }
    
    /// <summary>
    /// 去掉视频音轨
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Click_2(object sender, RoutedEventArgs e)
    {
      if (srcFilePath.Length == 0) return;
      string dstFilePath = fpath.GetDirectoryName(srcFilePath) + "\\" + fpath.GetFileNameWithoutExtension(srcFilePath) + "-out" + fpath.GetExtension(srcFilePath);
      // ffmpeg -i "demo.mp4" -vcodec copy -an "out.mp4"
      string command = "-i \"" + srcFilePath + "\" -vcodec copy -an \"" + dstFilePath + "\"";
      execute(command);
    }

    /// <summary>
    /// 提取视频音轨
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Click(object sender, RoutedEventArgs e)
    {
      if (srcFilePath.Length == 0) return;
      string dstFilePath = fpath.GetDirectoryName(srcFilePath) + "\\" + fpath.GetFileNameWithoutExtension(srcFilePath) + "-out" + ".mp3";
      // ffmpeg -i demo.mp4  out.mp3
      string command = "-i \"" + srcFilePath + "\"  \"" + dstFilePath + "\"";
      execute(command);
    }

    /// <summary>
    /// 调整音量
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Click_3(object sender, RoutedEventArgs e)
    {
      if (srcFilePath.Length == 0) return;
      if (inputVolume.Text.Length == 0) return;


      string dstFilePath = fpath.GetDirectoryName(srcFilePath) + "\\" + fpath.GetFileNameWithoutExtension(srcFilePath) + "-out" + fpath.GetExtension(srcFilePath);


      // 视频不变，减少音量
      // ffmpeg - i input.mp4 -vcodec copy -af "volume=-10dB"  out.mp4
      string command = "-i \"" + srcFilePath + "\" -vcodec copy -af \"volume=" + inputVolume.Text + "\" \"" + dstFilePath + "\"";
      execute(command);
    }

    private void Button_Click_4(object sender, RoutedEventArgs e)
    {
      if (srcFilePath.Length == 0) return;
      if (startSeconds.Text.Length == 0) return;

      string dstFilePath = fpath.GetDirectoryName(srcFilePath) + "\\" + fpath.GetFileNameWithoutExtension(srcFilePath) + "-out" + fpath.GetExtension(srcFilePath);

      // 从1小时到结尾
      // ffmpeg -ss 01:00:00 -i m.mp4 -c copy out.mp4
      string command = "-ss "+ startSeconds.Text + " -i \"" + srcFilePath + "\" -c copy \"" + dstFilePath + "\"";
      execute(command);

    }

    private void Button_Click_5(object sender, RoutedEventArgs e)
    {
      if (srcFilePath.Length == 0) return;
      string command = "\"" + srcFilePath + "\"";
      System.Diagnostics.Process.Start(ffplayExe, command);
    }
  }
}
