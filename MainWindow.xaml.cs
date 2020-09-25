using System;
using io = System.IO;
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
      // 1. 先在命令行PATH中找ffmpeg
      // 2. 其次在程序执行目录中ffmpeg
      // 3. 都没找到退出

      ffmpegExe = GetFullPath("ffmpeg.exe");
      ffplayExe = GetFullPath("ffplay.exe");

      if(ffmpegExe == null) ffmpegExe = AppDomain.CurrentDomain.BaseDirectory + "ffmpeg.exe";
      if (ffplayExe == null) ffplayExe = AppDomain.CurrentDomain.BaseDirectory + "ffplay.exe";

      if (System.IO.File.Exists(ffmpegExe) == false)
      {
        MessageBox.Show("未找到[ffmpeg.exe]，工具将无法使用", "错误",MessageBoxButton.OK, MessageBoxImage.Error);
        System.Windows.Application.Current.Shutdown();
      }
      if (System.IO.File.Exists(ffplayExe) == false)
      {
        MessageBox.Show("未找到[ffplay.exe]，播放功能无法使用", "错误",MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private string GetFullPath(string fileName)
    {
      if (io.File.Exists(fileName))
        return io.Path.GetFullPath(fileName);

      var values = Environment.GetEnvironmentVariable("PATH");
      foreach (var path in values.Split(io.Path.PathSeparator))
      {
        var fullPath = io.Path.Combine(path, fileName);
        if (io.File.Exists(fullPath))
          return fullPath;
      }
      return null;
    }

    private void Drop1(object sender, DragEventArgs e)
    {
      string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
      if (filenames.Length > 0)
      {
        srcFilePath = filenames[0];
        selectRes.Text = srcFilePath;
      }
    }

    private void Drop2(object sender, DragEventArgs e)
    {
      string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
      if (filenames.Length > 0)
      {
        inpytAudio.Text = filenames[0];
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
    /// 将处理后的资源，写入相同目录下
    /// </summary>
    /// <returns></returns>
    private string getOutputFilepath(string extension = "")
    {
      extension = extension.Length != 0 ? extension : io.Path.GetExtension(srcFilePath);
      string time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();

      // xxx/xxx/[time]-oldname.extension
      return io.Path.GetDirectoryName(srcFilePath) + "\\[" + time + "]-" + io.Path.GetFileNameWithoutExtension(srcFilePath) + extension;
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
      string dstFilePath = getOutputFilepath();
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
      string dstFilePath = getOutputFilepath(".mp3");
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
      string dstFilePath = getOutputFilepath();

      // 视频不变，减少音量
      // ffmpeg - i input.mp4 -vcodec copy -af "volume=-10dB"  out.mp4
      string command = "-i \"" + srcFilePath + "\" -vcodec copy -af \"volume=" + inputVolume.Text + "\" \"" + dstFilePath + "\"";
      execute(command);
    }
  
    /// <summary>
    /// 使用ffplay播放输入资源，视频，音频，图片，直播流...
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Click_5(object sender, RoutedEventArgs e)
    {
      if (srcFilePath.Length == 0) return;
      string command = "\"" + srcFilePath + "\"";
      System.Diagnostics.Process.Start(ffplayExe, command);
    }

    /// <summary>
    /// 音频填充视频
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Click_6(object sender, RoutedEventArgs e)
    {
      // 将10s视频和5s音频合并，输出视频有10s,音频将一直循环
      // ffmpeg -i input.mp4 -stream_loop -1 -i input.mp3 -c copy -map 0:v:0 -map 1:a:0 -shortest out.mp4

      if (srcFilePath.Length == 0) return;
      if (inpytAudio.Text.Length == 0) return;

      string dstFilePath = getOutputFilepath();

      // 从1小时到结尾
      // ffmpeg -ss 01:00:00 -i m.mp4 -c copy out.mp4
      string command = "-i \"" + srcFilePath + "\" -stream_loop -1 -i \"" + inpytAudio.Text + "\" -c copy -map 0:v:0 -map 1:a:0 -shortest \""+ dstFilePath +"\"";
      execute(command);
    }

    /// <summary>
    /// 视频填充音频 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Click_7(object sender, RoutedEventArgs e)
    {
      if (srcFilePath.Length == 0) return;
      if (inpytAudio.Text.Length == 0) return;

      string dstFilePath = getOutputFilepath();

      // 将5s视频和10s音频合并，输出视频有10s,视频将一直循环
      // ffmpeg -stream_loop -1 -i input.mp4 -i input.mp3 -c copy -map 0:v:0 -map 1:a:0 -shortest out.mp4

      string command = "-stream_loop -1 -i \""+srcFilePath+"\" -i \""+ inpytAudio.Text +"\" -c copy -map 0:v:0 -map 1:a:0 -shortest \""+ dstFilePath +"\"";
      execute(command);
    }
    
    /// <summary>
    /// 选择音频文件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Click_8(object sender, RoutedEventArgs e)
    {
      var openFileDialog = new Microsoft.Win32.OpenFileDialog()
      {
        Filter = "(*.*)|*.*"
      };
      if (openFileDialog.ShowDialog() == false) return;
      inpytAudio.Text = openFileDialog.FileName;
    }
    
    /// <summary>
    /// 从开始处裁剪指定时间
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Click_9(object sender, RoutedEventArgs e)
    {

      if (srcFilePath.Length == 0) return;
      string dstFilePath = getOutputFilepath();

      // ffmpeg -i input.mp4 -ss 00:00:00 -t 10 1.mp3
      string command = "-i \"" + srcFilePath + "\" -ss "+ c_1_0.Text +" -t "+ c_1_1.Text +" \"" + dstFilePath + "\"";
      execute(command);
    }

    /// <summary>
    /// 时间段裁剪
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Click_10(object sender, RoutedEventArgs e)
    {

      if (srcFilePath.Length == 0) return;
      string dstFilePath = getOutputFilepath();

      DateTime start = DateTime.Parse(c_2_0.Text);
      DateTime end = DateTime.Parse(c_2_1.Text);
      int t = (end.Second - start.Second);
      if(t < 0)
      {
        MessageBox.Show("切割时间不能为负!!!");
        return;
      }
      // ffmpeg -i input.mp4 -ss 00:00:00 -t 10 1.mp3
      string command = "-i \"" + srcFilePath + "\" -ss " + c_2_0.Text + " -t " + t.ToString() + " \"" + dstFilePath + "\"";
      execute(command);
    }
    
    /// <summary>
    /// 从指定时间道结束
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_Click_11(object sender, RoutedEventArgs e)
    {
      if (srcFilePath.Length == 0) return;
      string dstFilePath = getOutputFilepath();

      // 从1小时到结尾
      // ffmpeg -ss 01:00:00 -i m.mp4 -c copy out.mp4
      string command = "-ss "+ c_3_0.Text + " -i \"" + srcFilePath + "\" -c copy \"" + dstFilePath + "\"";
      execute(command);
    }
  }
}
