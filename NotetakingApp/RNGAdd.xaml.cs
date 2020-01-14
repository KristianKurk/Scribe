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
using BLL;
using Database;

namespace NotetakingApp
{
    /// <summary>
    /// Interaction logic for RNGAdd.xaml
    /// </summary>
    public partial class RNGAdd : Page
    {
        public RNGAdd()
        {
            InitializeComponent();
        }
        private void SaveText(object sender, RoutedEventArgs e)
        {
            string text = rngTB.Text;
            string title = rngTitle.Text;

            RandomGenerator rng = new RandomGenerator();
            rng.rng_title = title;
            rng.rng_content = text;
            DB.Add(rng);
        }
    }
}