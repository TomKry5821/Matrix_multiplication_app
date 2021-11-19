﻿using MAtrixMultiplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MAtrixMultiplicationWindow
{
    class Controller
    {
        public Controller() { }

        public void GenerateMatrixes(MainWindow window)
        {
            int m1Rows, m2Rows, m1Columns, m2Columns;
            if(this.CheckConditions(window, out string message, out  m1Rows, out m2Rows, out m1Columns, out m2Columns) == false)
            {
                MessageBox.Show(message);
                ClearView(window);
            }
            else
            {
                window.SetMatrix1(Matrix.GenerateMatrix(m1Rows, m1Columns));
                window.SetMatrix2(Matrix.GenerateMatrix(m2Rows, m2Columns));
                MessageBox.Show("Załadowano macierze!");
            }
        }

        public void Multiplication(MainWindow Window)
        {
            if (Window.GetMatrix1().GetColumns() != Window.GetMatrix2().GetRows())
            {
                MessageBox.Show("Podane w pliku macierze nie mogą zostać pomnożone!\nIlość kolumn macierzy pierwszej musi być równa ilości wierszy macierzy drugiej", "Błąd wymiarów");
                return;
            }
            string measuredTime = "";
            int rows, columns;
            rows = Window.GetMatrix1().GetRows();
            columns = Window.GetMatrix2().GetColumns();
            int size = Window.GetMatrix1().GetColumns();
            int columnsAfterAlign = (columns + ( 4 - columns % 4));
            if(Window.GetOption() == Option.Cpp)
            {
                Window.SetResultMatrix(new Matrix(rows, columns));
            }
            else
            {
                Window.SetResultMatrix(new Matrix(rows, columns));
            }
            switch (Window.GetMultithreading())
            {
                case Multithreading.OFF:
                    {
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        this.SingleThreadMultiplication(Window, size, columns, columnsAfterAlign, rows);
                        watch.Stop();
                        long elapsedMs = watch.ElapsedMilliseconds;
                        measuredTime = elapsedMs.ToString() + "ms";
                        break;
                    }
                case Multithreading.ON:
                    {
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        int threadsCount = Window.GetThreadsLenght();
                        this.MultiThreadMultiplication(Window, rows, threadsCount, columns, columnsAfterAlign);
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        measuredTime = elapsedMs.ToString() + "ms";
                        break;
                    }
            }
            MessageBox.Show("Czas Wykonywania - " + measuredTime + "\n wynikowa macierz zapisana w pliku wynik.txt");
        }

        public void SingleThreadMultiplication(MainWindow Window, int size, int columns, int columnsAfterAlign, int rows)
        {
            for (int rowsCount = 0; rowsCount < rows; rowsCount++)
            {
                unsafe
                {
                    fixed (int* resultRow = &Window.GetResultMatrix().matrix[rowsCount, 0])
                    fixed (int* rowToMultiply = &Window.GetMatrix1().matrix[rowsCount, 0])
                    fixed (int* colToMultiply = &Window.GetMatrix2().matrix[0, 0])
                        switch (Window.GetOption())
                        {
                            case Option.Cpp:
                                {
                                    MatrixMultiplication.App.CppMultiplication(resultRow, rowToMultiply, colToMultiply, columns, size);
                                    break;
                                }
                            case Option.Asm:
                                {
                                    int[] args = new int[3];
                                    args[0] = size; args[1] = columns; args[2] = columnsAfterAlign;
                                    fixed (int* argsPtr = &args[0])
                                        MatrixMultiplication.App.AsmMultiplication(resultRow, rowToMultiply, colToMultiply, size);
                                    break;
                                }
                        }
                }
            }
        }

        public void MultiThreadMultiplication(MainWindow Window, int rows, int threadsCount, int columns, int columnsAfterAlign)
        {
            int rowsCount = 0;
            for (int i = 0; i < threadsCount; i++)
            {
                Window.GetThreads()[i] = Window.StartTheThread(columns, rowsCount, rows, columnsAfterAlign);
                rowsCount++;
                if (i == (threadsCount - 1))
                {
                    if (rowsCount < rows) { i = 0; }
                }
                if (rowsCount == rows) { break; }
            }
            for (int i = 0; i < threadsCount; i++)
            {
                Window.GetThreads()[i].Join();
            }
        }

        public string LoadMatrixFromPath(out Matrix matrix, MainWindow window, string matrixPath)
        {
            if (matrixPath.Equals(null))
            {
                matrix = new Matrix(0);
                return "Wpisz ścieżkę do pliku!";
            }
            else if (matrixPath.Contains(".txt") == false)
            {
                matrix = new Matrix(0);
                return "Nie znaleziono pliku!";
            }
            else
            {
                matrix = Matrix.LoadMatrix(matrixPath);
                return "Udało sie!";
            }
        }

        public string LoadThreads(MainWindow window)
        {
            int numberOfThreads;
            try
            {
                numberOfThreads = Convert.ToInt32(window.NumberOfThreadsTextBox.Text);
            }
            catch (FormatException)
            {
                window.SetThreads(new Thread[0]);
                return "Zła liczba wątków!";
            }
            if (numberOfThreads == 0)
            {
                window.SetThreads(new Thread[0]);
                return "Zła liczba wątków!";
            }
            window.SetThreads(new Thread[numberOfThreads]);
            if (numberOfThreads == 1)
            {
                window.SetMultithreading(Multithreading.OFF);
            }
            else
            {
                window.SetMultithreading(Multithreading.ON);
            }
            return "Załadowano!";
        }

        public void ResetThreads(MainWindow window)
        {
            window.SetThreads(Array.Empty<Thread>());
        }

        public void ClearView(MainWindow window)
        {
            window.FirstMatrixPath.Text = "";
            window.SecondMatrixPath.Text = "";
            window.M1RowsTextBox.Text = "";
            window.M2RowsTextBox.Text = "";
            window.M1ColumnsTextBox.Text = "";
            window.M2ColumnsTextBox.Text = "";
            window.NumberOfThreadsTextBox.Text = "";
        }

        public void ClearMatrixes(MainWindow window)
        {
            window.GetMatrix1().ResetMatrix();
            window.GetMatrix2().ResetMatrix();
            window.GetResultMatrix().ResetMatrix();
        }

        public bool CheckConditions(MainWindow window, out string message, out int m1Rows, out int m2Rows, out int m1Columns, out int m2Columns)
        {
            if (window.M1RowsTextBox.Text == null || window.M1ColumnsTextBox.Text == null || window.M2ColumnsTextBox == null || window.M2RowsTextBox == null)
            {
                message = "Któryś z podanych wymiarów macierzy jest niepoprawmy!";
                m1Rows = m2Rows = m1Columns = m2Columns = 0;
                return false;
            }
            else
            {
                try
                {

                    m1Rows = Convert.ToInt32(window.M1RowsTextBox.Text);
                    m2Rows = Convert.ToInt32(window.M2RowsTextBox.Text);
                    m1Columns = Convert.ToInt32(window.M1ColumnsTextBox.Text);
                    m2Columns = Convert.ToInt32(window.M2ColumnsTextBox.Text);

                    if(m1Columns != m2Rows)
                    {
                        message = "Liczba kolumn w Macierzy pierwszej\nmusi być równa liczbie wierszy macierzy drugiej!";
                        m1Rows = m2Rows = m1Columns = m2Columns = 0;
                        return false;
                    }
                }
                catch (System.FormatException)
                {
                    message = "Podaj liczby!";
                    m1Rows = m2Rows = m1Columns = m2Columns = 0;
                    return false;
                }
            }
            message = null;
            return true;
        }
    }
}