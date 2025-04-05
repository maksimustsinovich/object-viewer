using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using ObjectViewer.Enum;
using ObjectViewer.Model;
using ObjectViewer.Parser;
using ObjectViewer.Render;
using Color = System.Drawing.Color;

namespace ObjectViewer;

public partial class MainWindow
{
    private WavefrontObject? _wavefrontObject;

    private WriteableBitmap? _writeableBitmap;

    private bool _isRotating;

    private Point _lastMousePosition;

    private Vector3 _translation = Vector3.Zero;

    private Vector3 _rotation = Vector3.Zero;

    private float _scale = 0.01f;

    private Vector3 _eye = new(1.0f, 1.0f, MathF.PI);

    private Vector3 _target = Vector3.Zero;

    private Vector3 _up = Vector3.UnitY;

    private float _zNear = 1f;

    private float _zFar = 100.0f;

    private Color _backgroundColor = Color.LightGray;

    private Color _gridColor = Color.DarkGray;

    private Color _modelColor = Color.Red;
    
    private float _translationSpeed = 0.1f;
    
    private bool _isAltPressed;
    
    private RenderType _renderType = RenderType.Lambert;
    
    private readonly DispatcherTimer _rotationTimer;
    
    private bool _isAutoRotating;
    
    private float _rotationSpeed = 0.25f;
    
    private static float CalculateModelSize(WavefrontObject wavefrontObject)
    {
        if (wavefrontObject.Vertices.Length == 0)
            return 1.0f;

        var minX = wavefrontObject.Vertices.Min(v => v.X);
        var maxX = wavefrontObject.Vertices.Max(v => v.X);
        var minY = wavefrontObject.Vertices.Min(v => v.Y);
        var maxY = wavefrontObject.Vertices.Max(v => v.Y);
        var minZ = wavefrontObject.Vertices.Min(v => v.Z);
        var maxZ = wavefrontObject.Vertices.Max(v => v.Z);

        var sizeX = maxX - minX;
        var sizeY = maxY - minY;
        var sizeZ = maxZ - minZ;

        return (sizeX + sizeY + sizeZ) / 3.0f;
    }

    private bool _isSmoothing = false;

    private void ToggleSmoothing()
    {
        _isSmoothing = !_isSmoothing;
    }
    
    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_backgroundColor, _gridColor, _modelColor, _renderType);

        if (settingsWindow.ShowDialog() != true) return;

        _backgroundColor = settingsWindow.BackgroundColor;
        _gridColor = settingsWindow.GridColor;
        _modelColor = settingsWindow.ModelColor;
        _renderType = settingsWindow.SelectedRenderType;

        RedrawModel();
    }

    public MainWindow()
    {
        InitializeComponent();

        var width = (int)(RenderImage.ActualWidth > 0 ? RenderImage.ActualWidth : 1920 + 250);
        var height = (int)(RenderImage.ActualHeight > 0 ? RenderImage.ActualHeight : 1080);

        _writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        RenderImage.Source = _writeableBitmap;

        WavefrontObjectBaseRenderer.DrawBackground(_writeableBitmap, _backgroundColor, _gridColor);

        RenderImage.MouseWheel += RenderImage_MouseWheel;
        RenderImage.MouseDown += RenderImage_MouseDown;
        RenderImage.MouseMove += RenderImage_MouseMove;
        RenderImage.MouseUp += RenderImage_MouseUp;
        
        KeyDown += MainWindow_KeyDown;
        KeyUp += MainWindow_KeyUp;
        
        _rotationTimer = new DispatcherTimer();
        _rotationTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / 60); 
        _rotationTimer.Tick += RotationTimer_Tick;
    }
    
    private void ToggleAutoRotation()
    {
        if (_isAutoRotating)
        {
            _rotationTimer.Stop();
            _isAutoRotating = false;
        }
        else
        {
            _rotationTimer.Start();
            _isAutoRotating = true;
        }
    }
    
    private void RotationTimer_Tick(object? sender, EventArgs e)
    {
        _rotation.Y += _rotationSpeed;
        RedrawModel(); 
    }
    
    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.LeftAlt or Key.RightAlt)
        {
            _isAltPressed = true;
        }

        switch (e.Key)
        {
            case Key.S:
                _translation.Y += _translationSpeed;
                break;

            case Key.W:
                _translation.Y -= _translationSpeed;
                break;

            case Key.D:
                _translation.X += _translationSpeed;
                break;

            case Key.A:
                _translation.X -= _translationSpeed;
                break;

            case Key.Up:
                _translation.Z -= _translationSpeed;
                break;

            case Key.Down:
                _translation.Z += _translationSpeed;
                break;
            
            case Key.R:
                ToggleAutoRotation();
                break;
            
            case Key.X: 
                _rotationSpeed += 0.25f;
                break;
            case Key.Z: 
                _rotationSpeed = Math.Max(0.05f, _rotationSpeed - 0.25f);
                break;
        }

        RedrawModel();
    }

    private void MainWindow_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.LeftAlt or Key.RightAlt)
        {
            _isAltPressed = false;
        }
    }

    private void RenderImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        _isRotating = true;
        _lastMousePosition = e.GetPosition(RenderImage);
        RenderImage.CaptureMouse();
    }

    private void RenderImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isRotating) return;

        var currentPosition = e.GetPosition(RenderImage);

        var deltaX = currentPosition.X - _lastMousePosition.X;
        var deltaY = currentPosition.Y - _lastMousePosition.Y;

        if (_isAltPressed)
        {
            _rotation.Z += (float)(deltaX * 0.01f);
        }
        else
        {
            _rotation.X -= (float)(deltaY * 0.01f);
            _rotation.Y += (float)(deltaX * 0.01f);
        }

        _lastMousePosition = currentPosition;

        RedrawModel();
    }

    private void RenderImage_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Released) return;
        _isRotating = false;
        RenderImage.ReleaseMouseCapture();
    }

    private void RenderImage_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
        {
            _scale *= 1.1f;
        }
        else
        {
            _scale /= 1.1f;

            if (_scale < 0.1f)
            {
                _scale = 0.1f;
            }
        }

        RedrawModel();
    }

    private void RedrawModel()
    {
        if (_writeableBitmap == null) return;

        try
        {
            var width = (int)(RenderImage.ActualWidth > 0 ? RenderImage.ActualWidth : 1920);
            var height = (int)(RenderImage.ActualHeight > 0 ? RenderImage.ActualHeight : 1080);

            if (_writeableBitmap.PixelWidth != width || _writeableBitmap.PixelHeight != height)
            {
                _writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                RenderImage.Source = _writeableBitmap;
            }

            WavefrontObjectBaseRenderer.DrawBackground(_writeableBitmap, _backgroundColor, _gridColor);

            if (_wavefrontObject == null) return;
            var wavefrontObject = WavefrontObjectBaseRenderer.UpdateWavefrontObject(
                _wavefrontObject,
                _translation, _rotation, _scale,
                _eye, _target, _up,
                _zNear, _zFar,
                width, height
            );

            var lightingModel = new PhongLightingModel(
                ambientCoefficient: 0.1f,
                diffuseCoefficient: 0.8f,
                specularCoefficient: 0.5f,
                shininess: 32.0f,
                ambientColor: Color.Gray,
                diffuseColor: _modelColor,
                specularColor: Color.White);
            
            switch (_renderType)
            {
                case RenderType.Wireframe:
                    WavefrontObjectWireframeRenderer.DrawWireframe(wavefrontObject, _writeableBitmap, _modelColor);
                    break;
                case RenderType.Lambert:
                    WavefrontObjectTriangleRenderer.DrawFilledTriangles(wavefrontObject, _writeableBitmap, _modelColor, _eye);
                    break;
                case RenderType.Phong:
                    WavefrontObjectPhongInterpolationRenderer.DrawFilledTriangles(wavefrontObject, _writeableBitmap, lightingModel, _eye);
                    break;
                
            }

        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to redraw the model:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Open OBJ File",
                Filter = "OBJ Files (*.obj)|*.obj|All Files (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != true) return;

            var filePath = openFileDialog.FileName;

            LoadingProgressBar.Visibility = Visibility.Visible;
            LoadingProgressBar.IsIndeterminate = true;

            try
            {
                _wavefrontObject = await Task.Run(() => LoadModelAsync(filePath));

                LoadingProgressBar.Visibility = Visibility.Collapsed;

                var modelSize = CalculateModelSize(_wavefrontObject);
                _translationSpeed = modelSize * 0.1f;

                var width = (int)(RenderImage.ActualWidth > 0 ? RenderImage.ActualWidth : 1920);
                var height = (int)(RenderImage.ActualHeight > 0 ? RenderImage.ActualHeight : 1080);

                _writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                RenderImage.Source = _writeableBitmap;

                ResetParameters();

                RedrawModel();
                
                var modelInfo = GetModelInfo(_wavefrontObject);
                MessageBox.Show(modelInfo, "Model Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load the model:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                LoadingProgressBar.Visibility = Visibility.Collapsed;
                LoadingProgressBar.IsIndeterminate = false;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to load the model:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private static WavefrontObject LoadModelAsync(string filePath)
    {
        return WavefrontObjectParser.Parse(filePath);
    }

    private void ResetParameters()
    {
        _translation = Vector3.Zero;
        _rotation = Vector3.Zero;
        _scale = 0.01f;

        _eye = new Vector3(1.0f, 1.0f, MathF.PI);
        _target = Vector3.Zero;
        _up = Vector3.UnitY;

        _zNear = 1f;
        _zFar = 100.0f;
    }
    
    private static string GetModelInfo(WavefrontObject wavefrontObject)
    {
        var vertexCount = wavefrontObject.Vertices.Length;
        var normalCount = wavefrontObject.VertexNormals.Length;
        var faceCount = wavefrontObject.Faces.Length;

        var minX = wavefrontObject.Vertices.Min(v => v.Vector.X);
        var maxX = wavefrontObject.Vertices.Max(v => v.Vector.X);
        var minY = wavefrontObject.Vertices.Min(v => v.Vector.Y);
        var maxY = wavefrontObject.Vertices.Max(v => v.Vector.Y);
        var minZ = wavefrontObject.Vertices.Min(v => v.Vector.Z);
        var maxZ = wavefrontObject.Vertices.Max(v => v.Vector.Z);

        var sizeX = maxX - minX;
        var sizeY = maxY - minY;
        var sizeZ = maxZ - minZ;

        return $"Model Information:\n" +
               $"Vertices: {vertexCount}\n" +
               $"Normals: {normalCount}\n" +
               $"Faces: {faceCount}\n" +
               $"Size X: {sizeX:F2}\n" +
               $"Size Y: {sizeY:F2}\n" +
               $"Size Z: {sizeZ:F2}\n";
    }
}