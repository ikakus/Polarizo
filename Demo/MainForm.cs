using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//using System.Windows.Controls.Primitives;

using Touchless.Vision.Camera;

namespace Demo
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (!DesignMode)
            {
                // Refresh the list of available cameras
                comboBoxCameras.Items.Clear();
                foreach (Camera cam in CameraService.AvailableCameras)
                    comboBoxCameras.Items.Add(cam);

                if (comboBoxCameras.Items.Count > 0)
                    comboBoxCameras.SelectedIndex = 0;
                startCapturing();
            }
        }

        
        PointData[,] PData;

        private CameraFrameSource _frameSource;
        private static Bitmap _latestFrame;
        private static Bitmap _capturedFrame;

        private bool m_Tracking;
        private int m_Oldx;
        private int m_Oldy;
        private int m_OldWidth;
        private int m_OldHeight;

        private Rectangle ninety_rect;
        private Rectangle forty_rect;
        private Rectangle Iplus_rect;
        private Rectangle Iminus_rect;


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            thrashOldCamera();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            thrashOldCamera();
        }


        public void DataFromRec(Bitmap bmp)// where all the magic begins
        {

            if (bmp == null)
               return;

            PData = new PointData[ninety_rect.Width, ninety_rect.Height];



            double k_C, k_90, k_45;

            //  k_C = Convert.ToInt32(textBox_kC.Text);
            k_C = Convert.ToDouble(textBox_kC.Text);
            k_90 = Convert.ToDouble(textBox_K90.Text);
            k_45 = Convert.ToDouble(textBox_k45.Text);

            double I_pc, I_mc, I_45, I_90;

            Color pi = bmp.GetPixel(new Point(20, 20).X,new Point(20,20).Y);
            int bGround = 0;//pi.R + pi.G + pi.B;
 

            for (int i = 0; i < ninety_rect.Width; i++)
                for (int j = 0; j < ninety_rect.Height; j++)
                {
  
                    // This is where ACUALLY calculations happen

                    Color pix = bmp.GetPixel(Iplus_rect.X + i, Iplus_rect.Y + j);
                    I_pc = (pix.R + pix.G + pix.B)-bGround;

                    pix = bmp.GetPixel(Iminus_rect.X + i, Iminus_rect.Y + j);
                    I_mc = (pix.R + pix.G + pix.B)-bGround;

                    pix = bmp.GetPixel(forty_rect.X + i, forty_rect.Y + j);
                    I_45 = (pix.R + pix.G + pix.B)-bGround;

                    pix = bmp.GetPixel(ninety_rect.X + i, ninety_rect.Y + j);
                    I_90 = (pix.R + pix.G + pix.B)-bGround;

                    double S_0, S_1, S_2, S_3, S_0G;


                    if (checkBox_NormalizedBool.Checked == true)
                    {
                        S_0G = k_C * (I_pc + I_mc);// Начальное значение стокса S0 
                    }
                    else S_0G = 1;

                    S_0 = (k_C * (I_pc + I_mc)) / S_0G;
                    S_1 = ((k_C * (I_pc + I_mc)) - 4 * k_90 * I_90) / S_0G;
                    S_2 = ((2 * k_45 * I_45) - (k_C * (I_pc + I_mc))) / S_0G;
                    S_3 = (k_C * (I_pc - I_mc)) / S_0G;

                    double Elipt, Azim, DOP;
                    string Polar;

                    Elipt = S_3 / (1 + Math.Sqrt(S_1 * S_1 + S_2 * S_2));

                    Azim = Math.Atan(S_2 / S_1) / 2;

                    DOP = Math.Sqrt(S_1 * S_1 + S_2 * S_2 + S_3 * S_3) / S_0;

                    if (Elipt > 1)
                    { Elipt = 1; }
                    if (Elipt < -1)
                    { Elipt = -1; }

                    if (DOP > 1)
                    { DOP = 1; }
                    if (DOP < 0)
                    { DOP = 0; }

                    if (S_3 > 0)
                    {
                        Polar = "Right";
                    }
                    else
                        Polar = "Left";

                    // Filling structure

                    PData[i, j] = new PointData(S_0, S_1, S_2, S_3, Azim, DOP, Elipt, Polar);// S_0;
                    //PData[i, j].Stokes_S1 = S_1;
                    //PData[i, j].Stokes_S2 = S_2;
                    //PData[i, j].Stokes_S3 = S_3;

                    //PData[i, j].Elipticity = Elipt;
                    //PData[i, j].Azimuth = Azim;
                    //PData[i, j].DOP = DOP;
                    //PData[i, j].Polariz = Polar;                          

                }

        }

        private void drawMapElip(object sender, PaintEventArgs e)
        {
            Bitmap map = new Bitmap(ninety_rect.Width, ninety_rect.Height);

            for (int i = 0; i < ninety_rect.Width; i++)
                for (int j = 0; j < ninety_rect.Height; j++)
                {
                    if (Math.Abs(PData[i, j].Elipticity) >= 0)
                        map.SetPixel(i, j, Color.Blue);
                    if (Math.Abs(PData[i, j].Elipticity) >= 0.25)
                        map.SetPixel(i, j, Color.Green);
                    if (Math.Abs(PData[i, j].Elipticity) >= 0.5)
                        map.SetPixel(i, j, Color.Orange);
                    if (Math.Abs(PData[i, j].Elipticity) >= 0.75)
                        map.SetPixel(i, j, Color.Red);
                    

                }


            //Bitmap map = new Bitmap(ninety_rect.Width, ninety_rect.Height);

            //for (int i = 0; i < ninety_rect.Width; i++)
            //{
            //    //map.SetPixel(i, 50, Color.BlanchedAlmond);
            //    for (int j = 0; j < ninety_rect.Height; j++)
            //    {
            //        map.SetPixel(i, j, Color.Red);
            //    }
            //}
            int mWidth, mHeight;
            if (map != null)
            {
                if (map.Width > map.Height)
                {
                    int k = pictureBox_DOP.Width / ninety_rect.Width;
                    mWidth = ninety_rect.Width * k;
                    mHeight = ninety_rect.Height * k;
                    //mWidth = pictureBox_Elipt.Width;
                    //mHeight = (mWidth * map.Height) / map.Width;
                    // MessageBox.Show(Convert.ToString(map.Height % map.Width));


                }
                else
                {
                    int k = pictureBox_DOP.Height / ninety_rect.Height;
                    mWidth = ninety_rect.Width * k;
                    mHeight = ninety_rect.Height * k;
                }
               
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                e.Graphics.DrawImage(map, 0, 0, mWidth, mHeight);
                

            }
        }

        int mW, mH;

        private void drawMapDOP(object sender, PaintEventArgs e)
        {
            Bitmap map = new Bitmap(ninety_rect.Width, ninety_rect.Height);

            for (int i = 0; i < ninety_rect.Width; i++)
                for (int j = 0; j < ninety_rect.Height; j++)
                {
                    if (Math.Abs(PData[i, j].DOP) >= 0)
                        map.SetPixel(i, j, Color.White);
                    if (Math.Abs(PData[i, j].DOP) >= 0.25)
                        map.SetPixel(i, j, Color.Gray);
                    if (Math.Abs(PData[i, j].DOP) >= 0.5)
                        map.SetPixel(i, j, Color.Chocolate);
                    if (Math.Abs(PData[i, j].DOP) >= 0.75)
                        map.SetPixel(i, j, Color.Black);
                   

                }


            int mWidth, mHeight;
            if (map != null)
            {
                if (map.Width > map.Height)
                {
                    int k = pictureBox_DOP.Width / ninety_rect.Width;
                    mWidth = ninety_rect.Width * k;
                    mHeight = ninety_rect.Height * k; 
                    //mWidth = pictureBox_Elipt.Width;
                    //mHeight = (mWidth * map.Height) / map.Width;
                    // MessageBox.Show(Convert.ToString(map.Height % map.Width));


                }
                else
                {
                    int k = pictureBox_DOP.Height / ninety_rect.Height;
                    mWidth = ninety_rect.Width * k;
                    mHeight = ninety_rect.Height * k;
                }

                mW = mWidth;
                mH = mHeight;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                e.Graphics.DrawImage(map, 0, 0, mWidth, mHeight);
               


            }
        }

        //Popup codePopup = new Popup();
        //TextBlock popupText = new TextBlock();
        //popupText.Text = "Popup Text";
        //popupText.Background = Brushes.LightBlue;
        //popupText.Foreground = Brushes.Blue;
        //codePopup.Child = popupText;
            

        public void pictureBox_Elipt_MouseOver(object sender, MouseEventArgs e)
        {
            double pW, recW,pH,recH;
            int k;

            if (ninety_rect.Width >= ninety_rect.Height)
            {
                k = pictureBox_DOP.Width / ninety_rect.Width;
                pW = (double)pictureBox_Elipt.Width;
                recW = (double)ninety_rect.Width;
               
            }
            else
            {
                k = pictureBox_DOP.Height / ninety_rect.Height;
                pH = (double)pictureBox_Elipt.Height;
                recH = (double)ninety_rect.Height;
                
            }

            int i, j;
            i = (e.X / k);
            j = (e.Y / k);

            if (e.X < ninety_rect.Width*k && e.Y< ninety_rect.Height*k)
            {
                textBox_S0.Text = PData[i, j].Stokes_S0.ToString();

                textBox_S1.Text = PData[i, j].Stokes_S1.ToString();
                textBox_S2.Text = PData[i, j].Stokes_S2.ToString();
                textBox_S3.Text = PData[i, j].Stokes_S3.ToString();

                textBox_Elipt.Text = PData[i, j].Elipticity.ToString();
                textBox_Azimuth.Text = PData[i, j].Azimuth.ToString();
                textBox_DOP.Text = PData[i, j].DOP.ToString();
                textBox_Poalrization.Text = PData[i, j].Polariz.ToString();
                         
                     
            }


            
              
                textBoxI_pc.Text = i.ToString();;
                textBoxI_45.Text = j.ToString(); 
                textBoxI_90.Text = k.ToString();
               
         
            
        }

        public int PointAverage(MouseEventArgs ev, int rad)
        {
            if (_capturedFrame != null)
            {



                int sum = 0;
                int num = 0;
                for (int i = 0; i < rad; i++)
                    for (int j = 0; j < rad; j++)
                    {

                        int R_x = (ev.X - rad / 2) + i;
                        int R_y = (ev.Y - rad / 2) + j;
                        Color pix = _capturedFrame.GetPixel(R_x, R_y);
                        sum += (pix.R + pix.G + pix.B);
                        num++;
                    }

                return sum / num;
            }

            else return -1;

        }

        


        public int PointAverageFromPoint(Point ev, int rad)
        {
            if (_capturedFrame != null)
            {



                int sum = 0;
                int num = 0;
                for (int i = 0; i < rad; i++)
                    for (int j = 0; j < rad; j++)
                    {

                        int R_x = (ev.X - rad / 2) + i;
                        int R_y = (ev.Y - rad / 2) + j;
                        Color pix = _capturedFrame.GetPixel(R_x, R_y);
                        sum += (pix.R + pix.G + pix.B);
                        num++;
                    }

                return sum / num;
            }

            else return -1;

        }

        

        private void btnStart_Click(object sender, EventArgs e)
        {
            // Early return if we've selected the current camera
            if (_frameSource != null && _frameSource.Camera == comboBoxCameras.SelectedItem)
                return;

            thrashOldCamera();
            startCapturing();
        }

        private void startCapturing()
        {
            try
            {
                Camera c = (Camera)comboBoxCameras.SelectedItem;
                setFrameSource(new CameraFrameSource(c));
                _frameSource.Camera.CaptureWidth = 320;
                _frameSource.Camera.CaptureHeight = 240;
                _frameSource.Camera.Fps = 25;
                _frameSource.NewFrame += OnImageCaptured;

                pictureBoxDisplay.Paint += new PaintEventHandler(drawLatestImage);
                pictureBox_video_big.Paint += new PaintEventHandler(draw_big_image);
                _frameSource.StartFrameCapture();
            }
            catch (Exception ex)
            {
                comboBoxCameras.Text = "Select A Camera";
                MessageBox.Show(ex.Message);
            }
        }

        private void drawLatestImage(object sender, PaintEventArgs e)
        {
            if (_latestFrame != null)
            {
                // Draw the latest image from the active camera
                e.Graphics.DrawImage(_latestFrame, 0, 0, pictureBoxDisplay.Width, pictureBoxDisplay.Height);
                //e.Graphics.DrawImage(_latestFrame, 0, 0, x, y);
            }
        }

        private void draw_big_image(object sender, PaintEventArgs e)// Для видео в большом окне
        {
            
            if (_latestFrame != null)
            {
                
                 e.Graphics.DrawImage(_latestFrame, 0, 0, pictureBox_image.Width, pictureBox_image.Height);
            }
 
        }

        


        private void draw_captured_image(object sender, PaintEventArgs e)// Для изображения в большом окне
        {

            if (_latestFrame != null)
            {

                e.Graphics.DrawImage(_capturedFrame, 0, 0, pictureBox_image.Width, pictureBox_image.Height);
               


            }

        }

        public void OnImageCaptured(Touchless.Vision.Contracts.IFrameSource frameSource, Touchless.Vision.Contracts.Frame frame, double fps)
        {
            _latestFrame = frame.Image;
            pictureBoxDisplay.Invalidate();
            pictureBox_video_big.Invalidate();
        }

        private void setFrameSource(CameraFrameSource cameraFrameSource)
        {
            if (_frameSource == cameraFrameSource)
                return;

            _frameSource = cameraFrameSource;
        }

        //

        private void thrashOldCamera()
        {
            // Trash the old camera
            if (_frameSource != null)
            {
                _frameSource.NewFrame -= OnImageCaptured;
                _frameSource.Camera.Dispose();
                setFrameSource(null);
                pictureBoxDisplay.Paint -= new PaintEventHandler(drawLatestImage);
            }
        }

        private void Go_capture(object sender, EventArgs e)
        {
            // Early return if we've selected the current camera
            if (_frameSource != null && _frameSource.Camera == comboBoxCameras.SelectedItem)
                return;

            thrashOldCamera();
            startCapturing();
        }

        //

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_capturedFrame == null)
                return;

            Bitmap current = (Bitmap)_capturedFrame.Clone();
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "*.bmp|*.bmp";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    current.Save(sfd.FileName);
                }
            }

            current.Dispose();
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            // snap camera
            if (_frameSource != null)
                _frameSource.Camera.ShowPropertiesDialog();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            _capturedFrame = _latestFrame;
            pictureBox_image.Paint += new PaintEventHandler(draw_captured_image);
            
                 
            pictureBox_image.Invalidate();
            
        }

        

        private void ColorRectangle(Bitmap bmp,Rectangle re, Color c)
        {
        //    if (bmp == null)
        //        return;

           // Rectangle r = pictureBox1.RectangleToScreen(re);

            for (int i = re.X; i <=re.X + re.Width; i++)
                for (int j = re.Y; j <=re.Y + re.Height; j++)
                {
                    bmp.SetPixel(i, j, c);
                }

           

            //for (int i = 10; i <= 40; i++)
            //    for (int j = 10; j <= 40; j++)
            //    {
            //        bmp.SetPixel(i, j, c);
            //    }
           
                
        }


        private void button_color_Click(object sender, EventArgs e)
        {
            Bitmap tmp;
            tmp = _capturedFrame;
            ColorRectangle(tmp, ninety_rect, Color.Aqua);

            ColorRectangle(tmp, forty_rect, Color.Bisque);

            ColorRectangle(tmp, Iminus_rect, Color.BurlyWood);

            ColorRectangle(tmp, Iplus_rect, Color.Crimson);

            pictureBox_image.Invalidate();
            // MessageBox.Show("asd");


        }

        private Rectangle DrawFrameRec(Rectangle rec, int x, int y)
        {


           
         

            rec.Width = ninety_rect.Width;
            rec.Height = ninety_rect.Height;

            rec.X = x - rec.Width / 2;
            rec.Y = y - rec.Height / 2;

            


            if (rec.X <= pictureBox_image.Left)
            {
                rec.X = 0;
            }
            else

                if (rec.X+rec.Width >= pictureBox_image.Width)
                {
                    rec.X = pictureBox_image.Width - rec.Width;                     
                }
                else
                    rec.Width = ninety_rect.Width; 

            if (rec.Y <= pictureBox_image.Top)
            {
                rec.Y = 0;
            }
            else

                if (rec.Y+rec.Height >= pictureBox_image.Height)
                {
                    rec.Y = pictureBox_image.Height - rec.Height; 
                }
                else
                    rec.Height = ninety_rect.Height;



            Rectangle r = pictureBox_image.RectangleToScreen(rec);
            ControlPaint.DrawReversibleFrame(r, this.BackColor, FrameStyle.Dashed);

            //ControlPaint.

           

            return rec;

                       

        }


      

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        { 
            if (checkBox4.Checked == true)
            {
                pictureBox_image.Invalidate();
                if (m_Tracking == false)
                {
                    m_Tracking = true;
                    Rectangle r;

                    Iplus_rect.X = 0;
                    Iplus_rect.Y = 0;
                    Iplus_rect.Height = 0;
                    Iplus_rect.Width = 0;

                    Iminus_rect = Iplus_rect;
                    forty_rect = Iplus_rect;

                    m_Oldx = e.X;
                    m_Oldy = e.Y;
                    m_OldHeight = 1;
                    m_OldWidth = 1;

                    ninety_rect.Height = m_OldHeight;
                    ninety_rect.Width = m_OldWidth;
                    ninety_rect.X = m_Oldx;
                    ninety_rect.Y = m_Oldy;

                    r = pictureBox_image.RectangleToScreen(ninety_rect);
                    ControlPaint.DrawReversibleFrame(r, this.BackColor, FrameStyle.Dashed);
                }
            }

            if (checkBox1.Checked == true)
            {
                if (m_Tracking == false)
                {
                    m_Tracking = true;
                    Rectangle r = pictureBox_image.RectangleToScreen(Iplus_rect);
                    ControlPaint.DrawReversibleFrame(r, this.BackColor, FrameStyle.Dashed);

                    Iplus_rect = DrawFrameRec(Iplus_rect, e.X, e.Y);

                }
            }

            if (checkBox2.Checked == true)
            {
                if (m_Tracking == false)
                {
                    m_Tracking = true;
                    Rectangle r = pictureBox_image.RectangleToScreen(Iminus_rect);
                    ControlPaint.DrawReversibleFrame(r, this.BackColor, FrameStyle.Dashed);

                    Iminus_rect = DrawFrameRec(Iminus_rect, e.X, e.Y);
                }
            }

            if (checkBox3.Checked == true)
            {
                if (m_Tracking == false)
                {
                    m_Tracking = true;
                    Rectangle r = pictureBox_image.RectangleToScreen(forty_rect);
                    ControlPaint.DrawReversibleFrame(r, this.BackColor, FrameStyle.Dashed);

                    forty_rect = DrawFrameRec(forty_rect, e.X, e.Y);
                }
            }

        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
           
                if (m_Tracking == true)
                {
                    if (checkBox4.Checked == true)
                    {
                        //Erase old frame.
                        Rectangle r;
                        r = pictureBox_image.RectangleToScreen(new Rectangle(m_Oldx, m_Oldy, m_OldWidth, m_OldHeight));
                        ControlPaint.DrawReversibleFrame(r, this.BackColor, FrameStyle.Dashed);

                        // Point p = pictureBox1.PointToScreen(new Point(e.X, e.Y));

                        if (e.X <= pictureBox_image.Left)
                        {
                            m_OldWidth = -pictureBox_image.Width + (pictureBox_image.Width - m_Oldx);
                        }
                        else

                            if (e.X >= pictureBox_image.Width)
                            {
                                m_OldWidth = pictureBox_image.Width - m_Oldx;
                            }
                            else
                                m_OldWidth = e.X - m_Oldx;

                        if (e.Y <= pictureBox_image.Top)
                        {
                            m_OldHeight = -pictureBox_image.Height + (pictureBox_image.Height - m_Oldy);

                        }
                        else

                            if (e.Y >= pictureBox_image.Height)
                            {
                                m_OldHeight = pictureBox_image.Height - m_Oldy;
                            }
                            else
                                m_OldHeight = e.Y - m_Oldy;

                        ninety_rect.Height = m_OldHeight;
                        ninety_rect.Width = m_OldWidth;
                        ninety_rect.X = m_Oldx;
                        ninety_rect.Y = m_Oldy;


                        textBox24.Text = Convert.ToString(ninety_rect.Width);
                        textBox23.Text = Convert.ToString(ninety_rect.Height);

                        r = pictureBox_image.RectangleToScreen(new Rectangle(m_Oldx, m_Oldy, m_OldWidth, m_OldHeight));
                        ControlPaint.DrawReversibleFrame(r, this.BackColor, FrameStyle.Dashed);

                    }

                    if (checkBox1.Checked == true)
                    {
                        Rectangle r = pictureBox_image.RectangleToScreen(Iplus_rect);
                        ControlPaint.DrawReversibleFrame(r, this.BackColor, FrameStyle.Dashed);

                        Iplus_rect = DrawFrameRec(Iplus_rect, e.X, e.Y);
                    }


                    if (checkBox2.Checked == true)
                    {                       
                        Rectangle r = pictureBox_image.RectangleToScreen(Iminus_rect);
                        ControlPaint.DrawReversibleFrame(r, this.BackColor, FrameStyle.Dashed);

                        Iminus_rect = DrawFrameRec(Iminus_rect, e.X, e.Y);
                    }


                    if (checkBox3.Checked == true)
                    {                     
                        Rectangle r = pictureBox_image.RectangleToScreen(forty_rect);
                        ControlPaint.DrawReversibleFrame(r, this.BackColor, FrameStyle.Dashed);

                        forty_rect = DrawFrameRec(forty_rect, e.X, e.Y);
                    }

            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {      
                if (m_Tracking == true)
                {
                    m_Tracking = false;                
                }
        }
        
      

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            
            if (_capturedFrame == null)
                return;
            int area;
            int bcGround;

            Point p = new Point(20, 20);

            area = Convert.ToInt32(textBox_Area.Text);

            bcGround = PointAverageFromPoint(p, area);
              if (checkBox1.Checked)
              {
                  
                 

                  textBox17.Text = e.X.ToString();
                  textBox18.Text = e.Y.ToString();
                  textBoxI_pc.Text = (PointAverage(e, area) - bcGround).ToString();

              }

              if (checkBox2.Checked)
              {
                  textBox20.Text = e.X.ToString();
                  textBox19.Text = e.Y.ToString();
                  textBoxI_mc.Text = (PointAverage(e, area) - bcGround).ToString();
              }

              if (checkBox3.Checked)
              {
                  textBox22.Text = e.X.ToString();
                  textBox21.Text = e.Y.ToString();
                  textBoxI_45.Text = (PointAverage(e, area) - bcGround).ToString();
              }

              if (checkBox4.Checked)
              {
                 // textBox24.Text = e.X.ToString();
                 // textBox23.Text = e.Y.ToString();
                  textBoxI_90.Text = (PointAverage(e, area) - bcGround).ToString();
              }

              

        }

       

        private void button3_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "*.bmp|*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _capturedFrame = new Bitmap(ofd.FileName);
                    pictureBox_image.Paint += new PaintEventHandler(draw_captured_image);
                    pictureBox_image.Invalidate();
                }
            }

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

            checkBox1.Checked = false;
            checkBox3.Checked = false;
            checkBox4.Checked = false;


        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

            checkBox3.Checked = false;
            checkBox2.Checked = false;
            checkBox4.Checked = false;



        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            checkBox1.Checked = false;
            checkBox2.Checked = false;
            checkBox4.Checked = false;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            checkBox1.Checked = false;
            checkBox2.Checked = false;
            checkBox3.Checked = false;
        }

        private void textBox21_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)// Calculate

        {

            if(_capturedFrame==null)
                return;

            DataFromRec(_capturedFrame);

            pictureBox_Elipt.Paint += new PaintEventHandler(drawMapElip);
            pictureBox_DOP.Paint += new PaintEventHandler(drawMapDOP);
            pictureBox_Elipt.Invalidate();
            pictureBox_DOP.Invalidate();

           
          //  if (textBoxI_pc.Text == "" || textBoxI_mc.Text == "" || textBoxI_45.Text == "" || textBoxI_90.Text == "")
          //  {
          //      return;
          //  }

          //  double k_C, k_90, k_45;

          ////  k_C = Convert.ToInt32(textBox_kC.Text);
          //  k_C = Convert.ToDouble(textBox_kC.Text);
          //  k_90 = Convert.ToDouble(textBox_K90.Text);
          //  k_45 = Convert.ToDouble(textBox_k45.Text);

          //  double I_pc, I_mc, I_45, I_90;

          //  I_pc = Convert.ToDouble(textBoxI_pc.Text);
          //  I_mc = Convert.ToDouble(textBoxI_mc.Text);
          //  I_45 = Convert.ToDouble(textBoxI_45.Text);
          //  I_90 = Convert.ToDouble(textBoxI_90.Text);

          //  double S_0, S_1, S_2, S_3, S_0G;


          //  if (checkBox_NormalizedBool.Checked == true)
          //  {
          //      S_0G = k_C * (I_pc + I_mc);// Начальное значение стокса S0 
          //  }
          //  else S_0G = 1;

          //  S_0 = (k_C * (I_pc + I_mc)) / S_0G;
          //  S_1 = ((k_C * (I_pc + I_mc)) - 4 * k_90 * I_90) / S_0G;
          //  S_2 = ((2 * k_45 * I_45) - (k_C * (I_pc + I_mc))) / S_0G;
          //  S_3 = (k_C * (I_pc - I_mc)) / S_0G;

          //  double Elipt, Azim, DOP;

          //  Elipt = S_3 / (1 + Math.Sqrt(S_1 * S_1 + S_2 * S_2));

          //  Azim = Math.Atan(S_2 / S_1) / 2;

          //  DOP = Math.Sqrt(S_1 * S_1 + S_2 * S_2 + S_3 * S_3) / S_0;

          //  if (Elipt > 1)
          //  {Elipt = 1;}
          //  if (Elipt < -1)
          //  {Elipt = -1;}

          //  if (DOP > 1)
          //  { DOP = 1; }
          //  if (DOP < 0)
          //  { DOP = 0; }

          //  //Setting Outputs

          //  if (S_3 > 0)
          //  {
          //      textBox_Poalrization.Text = "Right";
          //  }
          //  else
          //      textBox_Poalrization.Text = "Left";

          //  textBox_S0.Text = S_0.ToString();
          //  textBox_S1.Text = S_1.ToString();
          //  textBox_S2.Text = S_2.ToString();
          //  textBox_S3.Text = S_3.ToString();

          //  textBox_Elipt.Text = Elipt.ToString();
          //  textBox_Azimuth.Text = Azim.ToString();
          //  textBox_DOP.Text = DOP.ToString();

        }

        




        private void textBox12_TextChanged(object sender, EventArgs e)
        {

        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            pictureBox_Elipt.Paint += new PaintEventHandler(drawMapElip);
            pictureBox_DOP.Paint += new PaintEventHandler(drawMapDOP);
            pictureBox_Elipt.Invalidate();
            pictureBox_DOP.Invalidate();
        }

        
      
      
        
    }
}
