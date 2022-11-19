using System;
using io = System.IO;
using System.Windows;
using System.Text.RegularExpressions;
using System.Collections;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public string ffmpegExe;
        public string ffplayExe;
        public string input = "";
        public MainWindow()
        {
            InitializeComponent();
            // 1. 先在命令行PATH中找ffmpeg
            // 2. 其次在程序执行目录中ffmpeg
            // 3. 都没找到退出

            ffmpegExe = GetFullPath("ffmpeg.exe");
            ffplayExe = GetFullPath("ffplay.exe");

            if (ffmpegExe == null) ffmpegExe = AppDomain.CurrentDomain.BaseDirectory + "ffmpeg.exe";
            if (ffplayExe == null) ffplayExe = AppDomain.CurrentDomain.BaseDirectory + "ffplay.exe";

            if (System.IO.File.Exists(ffmpegExe) == false)
            {
                MessageBox.Show("未找到[ffmpeg.exe]，工具将无法使用", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown();
            }
            if (System.IO.File.Exists(ffplayExe) == false)
            {
                MessageBox.Show("未找到[ffplay.exe]，播放功能无法使用", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 返回唯一的时间戳
        /// </summary>
        /// <returns></returns>
        private string GetFileKey()
        {
            return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
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
                input = filenames[0];
                inputFile.Text = input;
            }
        }

        private void Drop2(object sender, DragEventArgs e)
        {
            string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (filenames.Length > 0)
            {
                inputAudio.Text = filenames[0];
            }
        }

        /// <summary>
        /// 带控制台，方便看到更多信息,和错误
        /// </summary>
        /// <param name="command"></param>
        private void Execute(string command)
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
        private string GetOutputFilepath(string extension = "")
        {
            if (input.Length == 0) return null;
            extension = extension.Length != 0 ? extension : io.Path.GetExtension(input);

            // xxx/xxx/[time]oldname.extension
            return io.Path.Combine(io.Path.GetDirectoryName(input), $"[{GetFileKey()}]-{io.Path.GetFileNameWithoutExtension(input)}{extension}");
        }

        /// <summary>
        /// 选择文件视频
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Open_File_Dialog(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "(*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == false) return;
            input = openFileDialog.FileName;
            inputFile.Text = input;
        }

        private bool CheckNotInputFile()
        {
            if (input.Length == 0)
            {
                MessageBox.Show("没有输入文件", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            };
            return false;
        }

        /// <summary>
        /// 去掉视频音轨
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Remove_Video_Audio(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string output = GetOutputFilepath();
            // ffmpeg -i "demo.mp4" -vcodec copy -an "out.mp4"
            string command = "-i \"" + input + "\" -vcodec copy -an \"" + output + "\"";
            Execute(command);
        }

        /// <summary>
        /// 提取视频音轨
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Get_Video_Audio(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string output = GetOutputFilepath(".mp3");
            // ffmpeg -i demo.mp4  out.mp3
            string command = "-i \"" + input + "\"  \"" + output + "\"";
            Execute(command);
        }

        /// <summary>
        /// 调整音量
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Set_Video_Volume(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            if (inputVolume.Text.Length == 0) return;
            string output = GetOutputFilepath();

            // 视频不变，减少音量
            // ffmpeg - i input.mp4 -vcodec copy -af "volume=-10dB"  out.mp4
            string command = "-i \"" + input + "\" -vcodec copy -af \"volume=" + inputVolume.Text + "\" \"" + output + "\"";
            Execute(command);
        }

        /// <summary>
        /// 使用ffplay播放输入资源，视频，音频，图片，直播流...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            ArrayList options = new ArrayList();
            if (isLoopPlay.IsChecked == true) options.Add("-loop 0");
            if (t_vn.IsChecked == true) options.Add("-vn");
            if (t_an.IsChecked == true) options.Add("-an");
            if (t_noborder.IsChecked == true) options.Add("-noborder");

            string[] size = t_size.Text.Split('x');
            if (size.Length == 2)
            {
                if (size[0].Length != 0) options.Add($"-x {size[0]}");
                if (size[1].Length != 0) options.Add($"-y {size[1]}");
            }

            options.Add($"\"{input}\"");

            string command = string.Join(" ", (string[])options.ToArray(Type.GetType("System.String")));

            System.Diagnostics.Process.Start(ffplayExe, command);
        }

        /// <summary>
        /// 音频填充视频
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_6(object sender, RoutedEventArgs e)
        {

            if (CheckNotInputFile() || inputAudio.Text.Length == 0) return;
            string output = GetOutputFilepath();

            // 将10s视频和5s音频合并，输出视频有10s,音频将一直循环
            // ffmpeg -i input.mp4 -stream_loop -1 -i input.mp3 -c copy -map 0:v:0 -map 1:a:0 -shortest out.mp4
            string command = $"-i \"{input}\" -stream_loop -1 -i \"{inputAudio.Text}\" -c copy -map 0:v:0 -map 1:a:0 -shortest \"{output}\"";

            if (isAmix.IsChecked == true)
            {
                // ffmpeg -i 4.mp4 -i a1.mp3 -c:v copy -filter_complex amix -map 0:v -map 0:a -map 1:a -shortest o.mp4
                command = $"-i \"{input}\" -stream_loop -1 -i \"{inputAudio.Text}\" -c:v copy -filter_complex amix -map 0:v:0 -map 0:a:0 -map 1:a:0 -shortest \"{output}\"";
            }
            Execute(command);
        }

        /// <summary>
        /// 视频填充音频 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            if (inputAudio.Text.Length == 0) return;

            string output = GetOutputFilepath();

            // 将5s视频和10s音频合并，输出视频有10s,视频将一直循环
            // ffmpeg -stream_loop -1 -i input.mp4 -i input.mp3 -c copy -map 0:v:0 -map 1:a:0 -shortest out.mp4
            string command = $"-stream_loop -1 -i \"{input}\" -i \"{inputAudio.Text}\" -c copy -map 0:v:0 -map 1:a:0 -shortest \"{output}\"";

            if (isAmix.IsChecked == true)
            {
                MessageBox.Show("此功能无法执行混音", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Execute(command);
        }

        /// <summary>
        /// 选择音频文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Open_Audio_Dialog(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "(*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == false) return;
            inputAudio.Text = openFileDialog.FileName;
        }

        /// <summary>
        /// 从开始处裁剪指定时间
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string output = GetOutputFilepath();

            // ffmpeg -i input.mp4 -ss 00:00:00 -t 10 -c copy 1.mp4
            string command = $"-i \"{input}\" -ss {c_1_0.Text} -t {c_1_1.Text} -c copy \"{output}\"";
            Execute(command);
        }

        /// <summary>
        /// 时间段裁剪
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_10(object sender, RoutedEventArgs e)
        {

            if (CheckNotInputFile()) return;
            string output = GetOutputFilepath();

            DateTime start = DateTime.Parse(c_2_0.Text);
            DateTime end = DateTime.Parse(c_2_1.Text);
            int t = (end.Second - start.Second);
            if (t < 0)
            {
                MessageBox.Show("切割时间不能为负!!!");
                return;
            }
            // ffmpeg -i input.mp4 -ss 00:00:00 -t 10 -c copy 1.mp3
            string command = $"-i \"{input}\" -ss {c_2_0.Text} -t {t} -c copy \"{output}\"";
            Execute(command);
        }

        /// <summary>
        /// 从指定时间道结束
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_11(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string output = GetOutputFilepath();

            // 从1小时到结尾
            // ffmpeg -ss 01:00:00 -i m.mp4 -c copy out.mp4
            string command = "-ss " + c_3_0.Text + " -i \"" + input + "\" -c copy \"" + output + "\"";
            Execute(command);
        }
    
        /// <summary>
        /// 查看ffmpeg裁剪视频相关命令详情
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.linuxuprising.com/2020/01/ffmpeg-how-to-crop-videos-with-examples.html");
        }

        /// <summary>
        /// 裁剪视频
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_12(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string output = GetOutputFilepath();

            // ffmpeg -i input.mp4 -filter:v "crop=w:h:x:y" -c:a copy output.mp4
            string command = "-i \"" + input + "\" -filter:v \"crop=" + c_iw.Text + ":" + c_ih.Text + ":" + c_ix.Text + ":" + c_iy.Text + "\" -c:a copy \"" + output + "\"";
            Execute(command);
        }

        /// <summary>
        /// 将视频分为多个片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_13(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string srcname = io.Path.GetFileNameWithoutExtension(input);
            string outDir = io.Path.Combine(io.Path.GetDirectoryName(input), $"[{GetFileKey()}]-{srcname}");
            io.Directory.CreateDirectory(outDir);

            string outsegmentname = io.Path.Combine(outDir, $"%0{s_num.Text}d.{Regex.Replace(s_ioext.Text, @"^\.+", "")}");

            // ffmpeg -i input.mp4 -c copy -map 0 -segment_time 00:01:00 -f segment out-%03d.ts
            string command = $"-i \"{input}\" -c copy -map 0 -segment_time {s_iss.Text} -f segment \"{outsegmentname}\"";
            Execute(command);
        }

        /// <summary>
        /// 用户提供合并文件，然后合并分片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_14(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string outfile = io.Path.Combine(io.Path.GetDirectoryName(input), $"new-{GetFileKey()}.mp4");

            // ffmpeg -f concat -safe 0 -i i.txt -c copy out.mp4
            string command = $"-f concat -safe 0 -i \"{input}\" -c copy \"{outfile}\"";
            Execute(command);
        }

        /// <summary>
        /// 生成视频合成配置文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_15(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string odir = io.Path.GetDirectoryName(input);
            string oextension = io.Path.GetExtension(input);
            string ocfgifile = io.Path.Combine(odir, "merge.txt");

            // (for %i in (*.ts) do @echo file 'file:%cd%\%i') > mylist.txt
            string command = $"/C (for %i in (\"{odir}\\*{oextension}\") do @echo file 'file:%i') > \"{ocfgifile}\"";
            System.Diagnostics.Process.Start("cmd.exe", command);
        }

        /// <summary>
        /// gif to mp4
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Convert_Gif_To_Mp4(object sender, RoutedEventArgs e)
        {

            if (CheckNotInputFile()) return;
            string output = GetOutputFilepath(".mp4");

            // ffmpeg -i input.gif -b:v 0 -crf 20 -vf "pad=ceil(iw/2)*2:ceil(ih/2)*2" -vcodec libx264 -pix_fmt yuv420p -f mp4 out2.mp4
            string command = $"-i \"" + input + "\" -b:v 0 -crf 20 -vf \"pad=ceil(iw/2)*2:ceil(ih/2)*2\" -vcodec libx264 -pix_fmt yuv420p -f mp4 \"" + output + "\"";
            Execute(command);
        }

        /// <summary>
        /// gif to webm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Convert_Gif_To_Webm(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string output = GetOutputFilepath(".webm");

            // ffmpeg -i input.gif -c vp9  -b:v 0 -crf 20 out3.webm
            string command = "-i \"" + input + "\" -c vp9  -b:v 0 -crf 40 \"" + output + "\"";
            Execute(command);
        }

        /// <summary>
        /// 视频转GIF
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Convert_Video_To_Gif(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string output = GetOutputFilepath(".gif");

            DateTime start = DateTime.Parse(gv_start.Text);
            DateTime end = DateTime.Parse(gv_end.Text);
            int t = (end.Second - start.Second);
            if (t < 0)
            {
                MessageBox.Show("时间不能为负!!!");
                return;
            }
            // ffmpeg -ss 30 -t 3 -i input.mp4 -vf "fps=10,scale=320:-1:flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse" -loop 0 output.gif
            string command = $"-ss {gv_start.Text} -t {t} -i \"{input}\" -vf \"fps={gv_fps.Text},scale={gv_w.Text}:{gv_h.Text}:flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\" -loop 0 \"{output}\"";
            Execute(command);
        }

        /// <summary>
        /// 视频中提取图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_20(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;

            DateTime start = DateTime.Parse(getimg_start.Text);
            DateTime end = DateTime.Parse(getimg_end.Text);
            int t = (end.Second - start.Second);
            if (t < 0)
            {
                MessageBox.Show("时间不能为负!!!");
                return;
            }

            string outDir = io.Path.Combine(io.Path.GetDirectoryName(input), $"[{GetFileKey()}]-images");
            io.Directory.CreateDirectory(outDir);
            string output = io.Path.Combine(outDir, $"%{getimg_num.Text}d.jpg");
            // ffmpeg -i 2.mp4 -r 1 -ss 00:00:20 -t 1 -f image2 o-%4d.jpg
            string command = $"-i \"{input}\" -r {getimg_fps.Text} -ss {getimg_start.Text} -t {t} -f image2 \"{output}\"";
            Execute(command);
        }

        /// <summary>
        /// 将帧图片合成为视频
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_21(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string inputImage = io.Path.Combine(io.Path.GetDirectoryName(input), $"%{getimg_num2.Text}d.jpg");
            string oout = io.Path.Combine(io.Path.GetDirectoryName(input), $"new-{GetFileKey()}.mp4");

            // ffmpeg -i %2d.jpg new.mp4
            string command = $"-i \"{inputImage}\" \"{oout}\"";
            Execute(command);
        }

        /// <summary>
        /// 下载m3u8到mp4
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Download_M3U8_To_Mp4(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;

            // 选择下载目录
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK || dialog.SelectedPath.Length == 0) return;

            // ffmpeg -i http://xxx/index.m3u8 -bsf:a aac_adtstoasc -c copy out.mp4
            string command = $"-i \"{input}\" -bsf:a aac_adtstoasc -c copy \"{io.Path.Combine(dialog.SelectedPath, $"{GetFileKey()}.mp4")}\"";
            Execute(command);
        }

        /// <summary>
        /// 手动输入 输入文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputFile_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            input = inputFile.Text;
        }

        /// <summary>
        /// 将输入文件转为MP4格式的视频文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Convert_Video_Format_To_Mp4(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string oout = GetOutputFilepath(".mp4");

            // ffmpeg -i output.flv -vcodec libx264 -pix_fmt yuv420p -c:a copy o5.mp4
            string command = $"-i \"{input}\" -vcodec libx264 -pix_fmt yuv420p -c:a copy \"{oout}\"";
            Execute(command);
        }

        /// <summary>
        /// 转图片格式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Convert_Image_Format(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string oout = GetOutputFilepath($".{Regex.Replace(image_new_type.Text, @"^\.+", "")}");
            
            // ffmpeg -i i.jpg o.webp
            string command = $"-i \"{input}\" \"{oout}\"";
            Execute(command);
        }

        /// <summary>
        /// 图片格式批量转换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Convert_Images_Format(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string newExtension = $".{Regex.Replace(image_new_type.Text, @"^\.+", "")}";

            string idir = io.Path.GetDirectoryName(input);
            string iextension = io.Path.GetExtension(input);
            string newinput = io.Path.Combine(idir, $"*{iextension}");

            string fkey = GetFileKey();
            string odir = io.Path.Combine(idir, fkey);
            io.Directory.CreateDirectory(odir);

            // (for %i in (*.jpg) do ffmpeg -i %i %~ni.webp)
            // (for %i in (.\xx\*.jpg) do ffmpeg -i %i %~dpitime\%~ni.webp)
            string command = $"/C \"(for %i in (\"{newinput}\") do \"{ffmpegExe}\" -i %i \"%~dpi{fkey}\\%~ni{newExtension}\")\"";
            System.Diagnostics.Process.Start("cmd.exe", command);
        }


        /// <summary>
        /// 转音频格式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Convert_Audio_Format(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile()) return;
            string oout = GetOutputFilepath($".{Regex.Replace(audio_new_type.Text, @"^\.+", "")}");
            
            // ffmpeg -i i.weba o.mp3
            string command = $"-i \"{input}\" \"{oout}\"";
            Execute(command);
        }


        /// <summary>
        /// 合并两个音频
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_25(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile() || inputAudio.Text.Length == 0) return;
            string oout = GetOutputFilepath(".mp3");
            // ffmpeg.exe -i a1.mp3 -i a2.mp3 -filter_complex amerge -c:a libmp3lame -q:a 4 out.mp3
            string command = $"-i \"{input}\" -i \"{inputAudio.Text}\" -filter_complex amerge -c:a libmp3lame -q:a 4 \"{oout}\"";
            Execute(command);
        }


        /// <summary>
        /// 转换视频帧率
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_27(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile() || toFps.Text.Length == 0) return;
            string oout = GetOutputFilepath();
            // ffmpeg -i 1.mp4 -vf "setpts=1.0*PTS" -c:a copy -r 30 o.mp4
            string command = $"-i \"{input}\" -vf \"setpts=1.0*PTS\" -c:a copy -r {toFps.Text} \"{oout}\"";
            Execute(command);
        }

        /// <summary>
        /// 转换视频的比特率
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_28(object sender, RoutedEventArgs e)
        {
            if (CheckNotInputFile() || toBitrate.Text.Length == 0) return;
            string oout = GetOutputFilepath();
            // ffmpeg -i 1.mp4 -b:v 1M -c:a copy o.mp4
            string command = $"-i \"{input}\" -b:v {toBitrate.Text} -c:a copy \"{oout}\"";
            Execute(command);
        }

        /// <summary>
        /// 播放时控制器详情
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Show_FFplay_Conctroller(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
              "f 切换全屏\n" +
              "p 却换暂停\n" +
              "m 切换静音\n" +
              "9,0 减少和增加音量\n" +
              "left/right 向后/向前搜索10秒\n" +
              "down/up 向后/向前搜索1分钟\n"
              );
        }
    }
}
