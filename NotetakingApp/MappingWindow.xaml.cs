﻿using System;
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
using Database;
using BLL;
using Microsoft.Win32;
using System.IO;

namespace NotetakingApp
{
    public partial class MappingWindow : Page
    {
        private int MAX_PIN_SIZE = 50;
        private int MAX_PIN_ZOOM_IN = 3;
        private float MAX_MAP_ZOOM_OUT = Properties.Settings.Default.MaxZoom;

        private Map currentMap = DB.GetMap(1);

        private Matrix initialMat;
        private Point firstPoint = new Point();
        private Point rightClickPoint = new Point();
        List<Button> pins = new List<Button>();
        List<Button> maps = new List<Button>();
        private double zoomPercentage = 1;
        private Canvas displayCanvas;
        private bool isHover = false;
        List<Map> tbd = new List<Map>();
        private List<Note> filteredNoteList;
        private int startWidth;
        private int startHeight;


        public MappingWindow()
        {
            InitializeComponent();

            initialMat = mapCanvas.RenderTransform.Value;
            BitmapImage img = DB.GetMap(1).LoadImage();
            imgSource.Source = img;

            //Many thanks to Luda for his help on this issue. <3
            //Even though it doesn't work (Actual width/height returns 0)
            startHeight = (int)((-img.PixelHeight + daddyCanvas.ActualHeight) / 2);
            startWidth = (int)((-img.PixelWidth + daddyCanvas.ActualWidth) / 2);


            Canvas.SetLeft(mapCanvas, startWidth);
            Canvas.SetTop(mapCanvas, startHeight);

            dbInit();
            init();

            filteredNoteList = DB.GetNotes();
            filteredListBox.ItemsSource = filteredNoteList;
        }

        public void init()
        {
            mapCanvas.MouseLeftButtonDown += (ss, ee) =>
            {
                firstPoint = ee.GetPosition(this);
                mapCanvas.CaptureMouse();
                if (displayCanvas != null)
                    SaveAndClosePin();

                AreYouSurePopup.IsOpen = false;
            };

            mapCanvas.PreviewMouseRightButtonDown += (ss, ee) =>
            {
                if (isHover == false) {
                    ContextMenu cm = this.FindResource("cmButton") as ContextMenu;
                    cm.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                    cm.IsOpen = true;
                    rightClickPoint = ee.GetPosition(mapCanvas);
                    Console.WriteLine("the button was clicked 1");
                }
            };

            mapCanvas.MouseWheel += (ss, ee) =>
            {
                Matrix mat = mapCanvas.RenderTransform.Value;
                Point mouse = ee.GetPosition(mapCanvas);
                zoomPercentage = mat.M11 / initialMat.M11;

                if (ee.Delta > 0)
                {
                    mat.ScaleAtPrepend(1.15, 1.15, mouse.X, mouse.Y);
                    ResizePins();
                }
                else if (zoomPercentage > MAX_MAP_ZOOM_OUT)
                {
                    mat.ScaleAtPrepend(1 / 1.15, 1 / 1.15, mouse.X, mouse.Y);
                    ResizePins();

                }
                else
                {
                    if (currentMap.parent_map_id != 0)
                    {
                        Map attachedMap = DB.GetMap(currentMap.parent_map_id);
                        BitmapImage img = attachedMap.LoadImage();
                        imgSource.Source = img;
                        currentMap = attachedMap;
                        foreach (Button map in maps)
                            pinCanvas.Children.Remove(map);
                        foreach (Button pin in pins)
                            pinCanvas.Children.Remove(pin);
                        maps.Clear();
                        pins.Clear();
                        dbInit();
                    }
                }

                MatrixTransform mtf = new MatrixTransform(mat);
                mapCanvas.RenderTransform = mtf;
            };


            mapCanvas.MouseMove += (ss, ee) =>
            {
                if (ee.LeftButton == MouseButtonState.Pressed &&
                    !firstPoint.Equals(new Point(-9999, -9999)))
                {
                    Point temp = ee.GetPosition(this);
                    Point res = new Point(firstPoint.X - temp.X, firstPoint.Y - temp.Y);

                    double tentativeLeft = Canvas.GetLeft(mapCanvas) - res.X;
                    double tentativeTop = Canvas.GetTop(mapCanvas) - res.Y;

                    Canvas.SetLeft(mapCanvas, tentativeLeft);
                    Canvas.SetTop(mapCanvas, tentativeTop);
                    firstPoint = temp;
                }
            };

            mapCanvas.MouseUp += (ss, ee) =>
            {
                firstPoint = new Point(-9999, -9999);
                mapCanvas.ReleaseMouseCapture();
            };
        }

        private void dbInit()
        {
            List<Pin> dbPins = DB.getPins();
            foreach (Pin dbPin in dbPins)
            {
                if (dbPin.parent_map_id == currentMap.map_id)
                {
                    Image pin = new Image();
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri("Assets/Pins/"+dbPin.pin_type+"pin.png", UriKind.Relative);
                    bitmap.EndInit();
                    pin.Source = bitmap;
                    pin.Stretch = Stretch.UniformToFill;

                    Button button = new Button();
                    button.Click += new RoutedEventHandler(Click_Pin);
                    button.MouseEnter += new MouseEventHandler(Mouse_Enter);
                    button.MouseLeave += new MouseEventHandler(Mouse_Leave);
                    button.PreviewMouseRightButtonDown += new MouseButtonEventHandler(Right_Click_Pin);
                    button.Background = Brushes.Transparent;
                    button.BorderThickness = new Thickness(0);

                    button.Name = "id" + dbPin.pin_id;
                    button.Content = pin;
                    Canvas.SetLeft(button, dbPin.pin_x);
                    Canvas.SetTop(button, dbPin.pin_y);

                    pins.Add(button);

                    pinCanvas.Children.Add(button);
                }
            }

            List<Map> dbMaps = DB.GetMaps();
            foreach (Map dbMap in dbMaps)
            {
                if (currentMap.map_id == dbMap.parent_map_id)
                {
                    Image map = new Image();
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri("Assets/Pins/map.png", UriKind.Relative);
                    bitmap.EndInit();
                    map.Source = bitmap;
                    map.Stretch = Stretch.UniformToFill;

                    Button button = new Button();
                    //button.Click += new RoutedEventHandler(Click_Map);
                    button.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(Click_Map);
                    button.PreviewMouseRightButtonDown += new MouseButtonEventHandler(Right_Click_Map);
                    button.MouseEnter += new MouseEventHandler(Mouse_Enter);
                    button.MouseLeave += new MouseEventHandler(Mouse_Leave);
                    button.Background = Brushes.Transparent;
                    button.BorderThickness = new Thickness(0);

                    button.Name = "mid" + dbMap.map_id;
                    button.Content = map;
                    Canvas.SetLeft(button, dbMap.map_x);
                    Canvas.SetTop(button, dbMap.map_y);

                    maps.Add(button);

                    pinCanvas.Children.Add(button);
                }
            }
            ResizePins();
        }

        private void Mouse_Leave(object sender, MouseEventArgs e)
        {
            isHover = false;
        }

        private void Mouse_Enter(object sender, MouseEventArgs e)
        {
            isHover = true;
            
        }

        private void CreatePin(Image pin, string type) {
            Button button = new Button();// { Style = FindResource("PinStyle") as Style }; 
            button.Click += new RoutedEventHandler(Click_Pin);
            button.MouseEnter += new MouseEventHandler(Mouse_Enter);
            button.MouseLeave += new MouseEventHandler(Mouse_Leave);
            button.PreviewMouseRightButtonDown += new MouseButtonEventHandler(Right_Click_Pin);
            button.Background = Brushes.Transparent;
            button.BorderThickness = new Thickness(0);



            Pin dbPin = new Pin();
            dbPin.pin_title = "Untitled";
            dbPin.pin_content = "";
            dbPin.pin_x = rightClickPoint.X;
            dbPin.pin_y = rightClickPoint.Y;
            dbPin.parent_map_id = currentMap.map_id;
            dbPin.pin_type = type;
            DB.Add(dbPin);

            button.Name = "id" + DB.getPins().Last().pin_id;
            button.Content = pin;

            pins.Add(button);
            ResizePins();

            pinCanvas.Children.Add(button);
        }


        private void Create_Pin_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Created a pin");

            Image pin = new Image();
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("Assets/Pins/playerpin.png", UriKind.Relative);
            bitmap.EndInit();
            pin.Source = bitmap;
            pin.Stretch = Stretch.UniformToFill;

            CreatePin(pin,"player");
        }

        private void Create_Pin_Click1(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Created a pin");

            Image pin = new Image();
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("Assets/Pins/npcpin.png", UriKind.Relative);
            bitmap.EndInit();
            pin.Source = bitmap;
            pin.Stretch = Stretch.UniformToFill;

            CreatePin(pin,"npc");
        }

        private void Create_Pin_Click2(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Created a pin");

            Image pin = new Image();
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("Assets/Pins/enemypin.png", UriKind.Relative);
            bitmap.EndInit();
            pin.Source = bitmap;
            pin.Stretch = Stretch.UniformToFill;

            CreatePin(pin,"enemy");
        }

        private void Create_Pin_Click3(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Created a pin");

            Image pin = new Image();
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("Assets/Pins/locationpin.png", UriKind.Relative);
            bitmap.EndInit();
            pin.Source = bitmap;
            pin.Stretch = Stretch.UniformToFill;

            CreatePin(pin,"location");
        }

        private void Create_Pin_Click4(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Created a pin");

            Image pin = new Image();
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("Assets/Pins/eventpin.png", UriKind.Relative);
            bitmap.EndInit();
            pin.Source = bitmap;
            pin.Stretch = Stretch.UniformToFill;

            CreatePin(pin,"event");
        }

        private void Create_Map_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Created a map");

            Image map = new Image();
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri("Assets/Pins/map.png", UriKind.Relative);
            bitmap.EndInit();
            map.Source = bitmap;
            map.Stretch = Stretch.UniformToFill;

            Button button = new Button();// { Style = FindResource("PinStyle") as Style };
            //button.Click += new RoutedEventHandler(Click_Map);
            button.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(Click_Map);
            button.PreviewMouseRightButtonDown += new MouseButtonEventHandler(Right_Click_Map);
            button.MouseEnter += new MouseEventHandler(Mouse_Enter);
            button.MouseLeave += new MouseEventHandler(Mouse_Leave);
            button.Background = Brushes.Transparent;
            button.BorderThickness = new Thickness(0);

            Map dbMap = new Map();
            dbMap.map_name = "Untitled";
            dbMap.map_x = rightClickPoint.X;
            dbMap.map_y = rightClickPoint.Y;
            dbMap.parent_map_id = currentMap.map_id;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select a Map Image File";
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.ReadOnlyChecked = true;
            openFileDialog.ShowReadOnly = true;
            openFileDialog.Filter = "Image files (*.png;*.jpeg)|*.png;*.jpeg|All files (*.*)|*.*";
            var result = openFileDialog.ShowDialog();
            if (openFileDialog.FileName != "")
            {
                byte[] buffer = File.ReadAllBytes(openFileDialog.FileName);
                dbMap.map_file = buffer;
                dbMap.map_name = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                DB.Add(dbMap);
                button.Name = "mid" + DB.GetMaps().Last().map_id;
                button.Content = map;

                maps.Add(button);

                pinCanvas.Children.Add(button);
                ResizePins();
            }
        }

        private void ResizePins()
        {
            if (zoomPercentage > 1)
            {
                if (zoomPercentage < MAX_PIN_ZOOM_IN)
                {

                    foreach (Button pin in pins)
                    {
                        pin.Width = MAX_PIN_SIZE / zoomPercentage;
                        pin.Height = MAX_PIN_SIZE / zoomPercentage;
                    }

                    foreach (Button map in maps)
                    {
                        map.Width = MAX_PIN_SIZE / zoomPercentage;
                        map.Height = MAX_PIN_SIZE / zoomPercentage;
                    }
                }
            }
            else
            {
                foreach (Button pin in pins)
                {
                    pin.Width = MAX_PIN_SIZE;
                    pin.Height = MAX_PIN_SIZE;
                }

                foreach (Button map in maps)
                {
                    map.Width = MAX_PIN_SIZE;
                    map.Height = MAX_PIN_SIZE;
                }
            }
            List<Pin> dbPins = DB.getPins();
            for (int i = 0; i < dbPins.Count; i++)
            {
                if (dbPins[i].parent_map_id == currentMap.map_id)
                {
                    Button pin = null;

                    foreach (Button mypin in pins)
                        if (int.Parse(mypin.Name.Substring(2)) == dbPins[i].pin_id)
                            pin = mypin;

                    Canvas.SetLeft(pin, dbPins[i].pin_x - pin.Width / 1.8);
                    Canvas.SetTop(pin, dbPins[i].pin_y - pin.Height / 1.2);
                }
            }
            List<Map> dbMaps = DB.GetMaps();

            for (int i = 0; i < dbMaps.Count; i++)
            {
                Console.WriteLine("count: " + maps.Count);
                Console.WriteLine(dbMaps[i].parent_map_id + " " + currentMap.map_id);

                if (dbMaps[i].parent_map_id == currentMap.map_id)
                {
                    Button map = null;

                    foreach (Button mymap in maps)
                        if (int.Parse(mymap.Name.Substring(3)) == dbMaps[i].map_id)
                            map = mymap;

                    Canvas.SetLeft(map, dbMaps[i].map_x - map.Width / 1.8);
                    Canvas.SetTop(map, dbMaps[i].map_y - map.Height / 1.2);
                }
            }
        }

        private void Click_Map(object sender, MouseButtonEventArgs e)
        {
            Button button = sender as Button;
            Map attachedMap = DB.GetMap(int.Parse(button.Name.Substring(3)));
            LoadMap(attachedMap);
        }

        public void LoadMap(Map attachedMap) {
            BitmapImage img = attachedMap.LoadImage();
            imgSource.Source = img;
            currentMap = attachedMap;
            foreach (Button map in maps)
                pinCanvas.Children.Remove(map);
            foreach (Button pin in pins)
                pinCanvas.Children.Remove(pin);
            maps.Clear();
            pins.Clear();
            dbInit();
            
            mapCanvas.RenderTransform.Value.Scale(1, 1);
        }

        private void Right_Click_Map(object sender, MouseButtonEventArgs e)
        {
            Button button = sender as Button;
            Button child = AreYouSure.Children[1] as Button;
            child.Name = button.Name;
            TextBlock text = AreYouSure.Children[0] as TextBlock;
            text.Text = "This action is irreversible. This will delete all inner maps and pins. Are you sure?";
            child.Click -= DeletePin2;
            child.Click += DeleteMap;
            AreYouSurePopup.PlacementTarget = sender as UIElement;
            AreYouSurePopup.IsOpen = true;
        }

        private void Right_Click_Pin(object sender, MouseButtonEventArgs e)
        {
            Button button = sender as Button;
            Button child = AreYouSure.Children[1] as Button;
            child.Name = button.Name;
            TextBlock text = AreYouSure.Children[0] as TextBlock;
            text.Text = "You are about to delete a pin. Are you sure?";
            child.Click -= DeleteMap;
            child.Click += DeletePin2;
            Console.WriteLine(child);
            AreYouSurePopup.PlacementTarget = sender as UIElement;
            AreYouSurePopup.IsOpen = true;


        }

        private void Click_Pin(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("pin clicked");
           
            Button button = sender as Button;
            Pin attachedPin = DB.GetPin(int.Parse(button.Name.Substring(2)));

            TextBox pinText = new TextBox();
            TextBox pinTitle = new TextBox();
            if (attachedPin.pin_content != null)
                pinText.Text = attachedPin.pin_content;
            else
                pinText.Text = "";
            pinText.AcceptsReturn = true;
            pinText.TextWrapping = TextWrapping.Wrap;
            pinText.MaxWidth = 200;
            pinText.Width = 200;
            pinText.MinLines = 3;
            pinText.MaxLines = 3;
            pinText.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

            pinTitle.Width = 150;
            if (attachedPin.pin_title != null)
                pinTitle.Text = attachedPin.pin_title;
            else
                pinTitle.Text = "Enter a title";
            pinTitle.MinLines = 1;
            pinTitle.MaxLines = 1;
            pinTitle.Height = 23;

            Button addNote = new Button();
            addNote.ClickMode = ClickMode.Press;
            addNote.ToolTip = "Add a note.";
            addNote.Name = "nt" + button.Name.Substring(2);
            addNote.Click += AddNote;
            if (attachedPin.attached_note_id == 0)
                addNote.Content = new TextBlock() { Text = "Add a Note." };
            else
                addNote.Content = new TextBlock() { Text = DB.GetNote(attachedPin.attached_note_id).note_title };
            addNote.Width = 175;

            Button deleteNote = new Button();
            deleteNote.ClickMode = ClickMode.Press;
            deleteNote.Name = "nt" + button.Name.Substring(2);
            deleteNote.Click += DeleteNote;
            deleteNote.ToolTip = "Delete a note.";
            deleteNote.Tag = addNote;
            deleteNote.Width = 20;

            Button closeButton = new Button();
            closeButton.Click += new RoutedEventHandler(SaveAndCloseButton);
            closeButton.ClickMode = ClickMode.Press;
            closeButton.Width = 22;
            closeButton.ToolTip = "Close Pin";

            Image closeImage = new Image();
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri("Assets/Navigation/close.png", UriKind.Relative);
            bmp.EndInit();
            closeImage.Source = bmp;
            closeButton.Content = closeImage;

            Image closeImage2 = new Image();
            closeImage2.Source = bmp;
            deleteNote.Content = closeImage2;

            Button deleteButton = new Button();
            deleteButton.Click += new RoutedEventHandler(DeletePin);
            deleteButton.ClickMode = ClickMode.Press;
            deleteButton.Width = 22;
            deleteButton.ToolTip = "Delete Pin";

            Image deleteImage = new Image();
            BitmapImage bmp2 = new BitmapImage();
            bmp2.BeginInit();
            bmp2.UriSource = new Uri("Assets/Navigation/delete.png", UriKind.Relative);
            bmp2.EndInit();
            deleteImage.Source = bmp2;
            deleteButton.Content = deleteImage;

            Canvas textCanvas = new Canvas();
            textCanvas.Children.Add(pinTitle);
            textCanvas.Children.Add(pinText);
            textCanvas.Children.Add(deleteButton);
            textCanvas.Children.Add(closeButton);
            textCanvas.Children.Add(addNote);
            textCanvas.Children.Add(deleteNote);
            textCanvas.Height = 75;
            textCanvas.Width = 200;
            Canvas.SetTop(pinText, 25);
            Canvas.SetTop(pinTitle, 0);
            Canvas.SetLeft(closeButton, 178);
            Canvas.SetLeft(deleteButton, 153);
            Canvas.SetTop(addNote, 75);
            Canvas.SetTop(deleteNote, 75);
            Canvas.SetLeft(deleteNote, 180);
            textCanvas.Name = button.Name;

            Canvas.SetLeft(textCanvas, attachedPin.pin_x);
            Canvas.SetTop(textCanvas, attachedPin.pin_y);
            pinCanvas.Children.Add(textCanvas);

            if (displayCanvas == null)
                displayCanvas = textCanvas;
            else
            {
                SaveAndClosePin();
                displayCanvas = textCanvas;
            }
        }

        private void SaveAndClosePin()
        {
            Pin attachedPin = DB.GetPin(Int32.Parse(displayCanvas.Name.Substring(2)));

            TextBox titlebox = displayCanvas.Children[0] as TextBox;
            string title = titlebox.Text;
            attachedPin.pin_title = title;

            TextBox textbox = displayCanvas.Children[1] as TextBox;
            string text = textbox.Text;
            attachedPin.pin_content = text;

            DB.Update(attachedPin);
            pinCanvas.Children.Remove(displayCanvas);
            pinpopup.IsOpen = false;
        }

        private void SaveAndCloseButton(object sender, RoutedEventArgs e)
        {
            SaveAndClosePin();
        }

        private void DeletePin(object sender, RoutedEventArgs e)
        {
            //Called by the button.
            Pin attachedPin = DB.GetPin(Int32.Parse(displayCanvas.Name.Substring(2)));
            DB.DeletePin(attachedPin.pin_id);
            pinCanvas.Children.Remove(displayCanvas);
            Button pin = null;

            foreach (Button mypin in pins)
                if (int.Parse(mypin.Name.Substring(2)) == attachedPin.pin_id)
                    pin = mypin;

            pinCanvas.Children.Remove(pin);
        }

        private void DeletePin2(object sender, RoutedEventArgs e)
        {
            //Called by right clicking.
            Button button = sender as Button;
            Pin attachedPin = DB.GetPin(Int32.Parse(button.Name.Substring(2)));
            if (attachedPin != null)
            {
                Button pin = null;

                foreach (Button mypin in pins)
                    if (int.Parse(mypin.Name.Substring(2)) == attachedPin.pin_id)
                        pin = mypin;

                DB.DeletePin(attachedPin.pin_id);
                pinCanvas.Children.Remove(pin);
                AreYouSurePopup.IsOpen = false;
            }
        }

        private void DeleteMap(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Map mapToBeDeleted = DB.GetMap(int.Parse(button.Name.Substring(3)));
            if (mapToBeDeleted != null)
            {
                tbd.Clear();
                RecursiveGetMapChildren(mapToBeDeleted);
                tbd.Add(mapToBeDeleted);

                foreach (Pin pin in DB.getPins())
                {
                    foreach (Map mymap in tbd)
                    {
                        if (pin.parent_map_id == mymap.map_id)
                            DB.DeletePin(pin.pin_id);
                    }
                }

                foreach (Map mymap in tbd)
                {
                    DB.DeleteMap(mymap.map_id);
                }

                Button map = null;

                foreach (Button mymap in maps)
                    if (int.Parse(mymap.Name.Substring(3)) == mapToBeDeleted.map_id)
                        map = mymap;
                pinCanvas.Children.Remove(map);
                AreYouSurePopup.IsOpen = false;
            }
        }

        private void RecursiveGetMapChildren(Map mapToBeDeleted)
        {
            foreach (Map map in DB.GetMaps()) {
                if (map.parent_map_id == mapToBeDeleted.map_id) {
                    tbd.Add(map);
                    RecursiveGetMapChildren(map);
                }
            }
        }

        private void HidePanel(object sender, RoutedEventArgs e)
        {
            AreYouSurePopup.IsOpen = false;
        }

        private void AddNote(object sender, RoutedEventArgs e) {
            Pin myPin = DB.GetPin(int.Parse(((Button)(sender)).Name.Substring(2)));

            if (!pinpopup.IsOpen)
            {
                if (myPin.attached_note_id == 0)
                {
                    pinpopup.PlacementTarget = (UIElement)sender;
                    pinpopup.IsOpen = true;
                    pinpopup.Tag = sender;
                }
                else
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.GetType() == typeof(MainWindow))
                        {
                            Properties.Settings.Default.currentNote = DB.GetNote(myPin.attached_note_id);
                            (window as MainWindow).main.Content = new Note_takingWindow();
                            ((MainWindow)window).disabledButton = "noteNavButton";
                            ((MainWindow)window).DisableButton("noteNavButton");
                        }
                    }
                }
            }
            else {
                pinpopup.IsOpen = false;
            }
        }

        private void DeleteNote(object sender, RoutedEventArgs e)
        {
            Pin myPin = DB.GetPin(int.Parse(((Button)(sender)).Name.Substring(2)));
            myPin.attached_note_id = 0;
            DB.Update(myPin);
            ((Button)((Button)(sender)).Tag).Content = "Add a Note.";
            pinpopup.IsOpen = false;
        }

        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchBar.Text != null && SearchBar.Text != "" && DB.GetNotes() != null)
            {
                filteredNoteList.Clear();
                foreach (Note n in DB.GetNotes())
                {
                    if (n.note_title.ToUpper().StartsWith(SearchBar.Text.ToUpper()))
                    {
                        filteredNoteList.Add(n);
                    }
                }
                Console.WriteLine(filteredNoteList.Count);
                filteredListBox.ItemsSource = null;
                filteredListBox.ItemsSource = filteredNoteList;
            }
            else
            {
                filteredNoteList = DB.GetNotes();
                filteredListBox.ItemsSource = null;
                filteredListBox.ItemsSource = filteredNoteList;
            }
        }

        private void FilteredListBox_Selected(object sender, SelectionChangedEventArgs e)
        {
            Note n = filteredListBox.SelectedItem as Note;
            Pin p = DB.GetPin(int.Parse(((Button)(pinpopup.Tag)).Name.Substring(2)));
            ((Button)(pinpopup.Tag)).Content = n.note_title;
            p.attached_note_id = n.note_id;
            DB.Update(p);
            pinpopup.IsOpen = false;
        }
    }
}
