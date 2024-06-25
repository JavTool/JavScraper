using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JavScraper.App
{
    public partial class FormMain : Form
    {
        public int threadNum { get; set; }          // 进程
        public bool[] threadStatus { get; set; }    // 每个线程结束标志
        public string[] fileNames { get; set; }     // 每个线程接收文件的文件名
        public int[] StartPos { get; set; }     // 每个线程接收文件的起始位置
        public int[] fileSize { get; set; }         // 每个线程接收文件的大小
        public string Url { get; set; }             // 接受文件的URL
        public bool HasMerge { get; set; }           // 文件合并标志

        public FormMain()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <remarks>
        /// 每个线程平均分配文件大小，剩余部分由最后一个线程完成
        /// </remarks>
        /// <param name="filesize"></param>
        private void Init(long filesize)
        {
            threadStatus = new bool[threadNum];
            fileNames = new string[threadNum];
            StartPos = new int[threadNum];
            fileSize = new int[threadNum];
            int filethread = (int)filesize / threadNum;
            int filethreade = filethread + (int)filesize % threadNum;
            for (int i = 0; i < threadNum; i++)
            {
                threadStatus[i] = false;
                fileNames[i] = i.ToString() + ".dat";
                if (i < threadNum - 1)
                {
                    StartPos[i] = filethread * i;
                    fileSize[i] = filethread - 1;
                }
                else
                {
                    StartPos[i] = filethread * i;
                    fileSize[i] = filethreade - 1;
                }
            }
        }
        /// <summary>
        /// 合并文件
        /// </summary>
        public void MergeFile()
        {
            while (true)
            {
                HasMerge = true;
                for (int i = 0; i < threadNum; i++)
                {
                    if (threadStatus[i] == false) // 若有未结束线程，则等待
                    {
                        HasMerge = false;
                        System.Threading.Thread.Sleep(100);
                        break;
                    }
                }
                if (HasMerge == true) // 否则，停止等待
                    break;
            }

            int bufferSize = 512;
            int readSize;
            string downFileNamePath = "";
            byte[] bytes = new byte[bufferSize];
            FileStream fs = new FileStream(downFileNamePath, FileMode.Create);
            FileStream fsTmp = null;

            for (int k = 0; k < threadNum; k++)
            {
                fsTmp = new FileStream(fileNames[k], FileMode.Open);
                while (true)
                {
                    readSize = fsTmp.Read(bytes, 0, bufferSize);
                    if (readSize > 0)
                        fs.Write(bytes, 0, readSize);
                    else
                        break;
                }
                fsTmp.Close();
            }
            fs.Close();
            MessageBox.Show("接收完毕!!!");
        }

        private void optionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormOption formOption = new FormOption();
            formOption.ShowDialog(this);

        }

        private void btnStart_Click(object sender, EventArgs e)
        {

        }

        private void btnStop_Click(object sender, EventArgs e)
        {

        }

        private void btnTest_Click(object sender, EventArgs e)
        {

        }
    }
}
