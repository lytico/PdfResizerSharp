using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Gtk;

namespace PdfResizer {

    class MainWindow : Window {

        private readonly HeaderBar _headerBar;
        private Label ExecuteState;

        public MainWindow() : base(WindowType.Toplevel) {
            WindowPosition = WindowPosition.Center;
            DefaultSize = new Gdk.Size(400, 400);

            _headerBar = new HeaderBar();
            _headerBar.ShowCloseButton = true;
            _headerBar.Title = nameof(PdfResizer);

            Titlebar = _headerBar;
            var content = new Box(Orientation.Vertical, 10);
            content.Halign = Align.Fill;
            content.Valign = Align.Start;

            var inputFile = default(string);
            var outputFile = default(string);

            var inputView = new Box(Orientation.Vertical, 0);
            var inputFileLabel = new Label("Input File:");

            var inputButton = new FileChooserButton("input", FileChooserAction.Open) {
                Filter = new FileFilter() {
                    Name = "PDF",

                }

            };

            inputButton.Filter.AddPattern("*.pdf");

            inputView.PackStart(inputFileLabel, true, true, 0);
            inputView.PackStart(inputButton, true, true, 0);

            var options = new string[] {
                "screen",
                "ebook",
                "prepress",
                "printer",
                "default"

            };

            var option = options[0];

            var outputView = new Box(Orientation.Vertical, 0);
            var outputLabel = new Label("Output File:");
            inputView.PackStart(outputLabel, true, true, 0);

            var outputFileView = new Label(outputFile);
            inputView.PackStart(outputFileView, true, true, 0);

            void OutputFile(string inputFile) {
                outputFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(inputFile),
                    $"{System.IO.Path.GetFileNameWithoutExtension(inputFile)}.resized.{option}.pdf");

                outputFileView.Text = System.IO.Path.GetFileName(outputFile);
                outputFileView.TooltipText = outputFile;
                outputView.HasTooltip = true;
                outputFileView.QueueDraw();
            }

            inputButton.FileSet += (sender, args) => {
                inputFile = inputButton.Filename;

                OutputFile(inputFile);

            };

            var optionsView = new Box(Orientation.Vertical, 0);

            var optionsBox = new ComboBox(options);

            optionsBox.Active = 0;

            optionsBox.Changed += (sender, args) => {
                option = options[optionsBox.Active];
                OutputFile(inputFile);
            };

            outputView.PackStart(new Label("Pdf Option:"), false, true, 0);
            outputView.PackStart(optionsBox, true, true, 0);

            var executeView = new Box(Orientation.Vertical, 0);

            var executeButton = new Button {
                Image = Image.NewFromIconName("system-run-symbolic", IconSize.Button),
                AlwaysShowImage = true,
                ImagePosition = PositionType.Left,
                Label = "Resize"
            };

            executeButton.Clicked += (sender, args) => {
                ExecuteResize(inputFile, outputFile, option);
            };

            ExecuteState = new Label { };

            executeView.PackStart(executeButton, true, true, 0);
            executeView.PackStart(ExecuteState, true, true, 0);

            content.PackStart(inputView, false, false, 10);
            content.PackStart(outputView, false, false, 10);
            content.PackStart(optionsView, false, false, 10);
            content.PackStart(executeView, false, false, 10);

            Child = content;
            Destroyed += (sender, e) => Application.Quit();

        }

        private bool isRunning = false;

        public void ExecuteResize(string inputfile, string outputfile, string pdfOption) {

            void Message(string message) {
                Application.Invoke((o, eventArgs) => {
                    ExecuteState.Text = message;
                    ExecuteState.QueueDraw();
                    DispatchPendingEvents();
                });

                Task.Yield();
            }

            try {
                if (isRunning) {
                    throw new Exception("Resizing is running");
                }

                if (string.IsNullOrEmpty(pdfOption)) {
                    throw new Exception($"no {nameof(pdfOption)} set");
                }

                if (string.IsNullOrEmpty(inputfile)) {
                    throw new FileNotFoundException($"no {nameof(inputfile)}");
                }

                if (!File.Exists(inputfile)) {
                    throw new FileNotFoundException($"{nameof(inputfile)} {inputfile} not found");
                }

                var process = new Process();

                // gs -sDEVICE=pdfwrite -dCompatibilityLevel=1.4 -dPDFSETTINGS=/screen -dNOPAUSE -dQUIET -dBATCH -sOutputFile=output.pdf input.pdf
                process.StartInfo = new ProcessStartInfo() {
                    FileName = "gs",
                    ArgumentList = {
                        "-sDEVICE=pdfwrite",
                        "-dCompatibilityLevel=1.4",
                        $"-dPDFSETTINGS=/{pdfOption}",
                        "-dNOPAUSE",
                        "-dBATCH",
                        $"-sOutputFile={outputfile}",
                        inputfile

                    },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };

                process.Exited += (sender, args) => {
                    Message($"Resizing finished:" +
                            $"\n{inputfile} ({new FileInfo(inputfile).Length / (1024 * 1024):N2} mb) -> " +
                            $"\n{outputfile} ({new FileInfo(outputfile).Length / (1024 * 1024):N2} mb)");
                };

                process.OutputDataReceived += (sender, args) => {
                    Message(args.Data);
                };

                process.ErrorDataReceived += (sender, args) => {
                    Message(args.Data);
                };

                Task.Run(async () => await RunProcess(process));

            } catch (Exception e) {
                using var error = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Cancel,
                    false, e.Message);

                error.Run();

            } finally { }

        }

        async Task RunProcess(Process process) {
            try {
                isRunning = true;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();
            } catch (Exception e) {
                using var error = new MessageDialog(this, DialogFlags.Modal, MessageType.Error, ButtonsType.Cancel,
                    false, e.Message);

                error.Run();

            } finally {
                isRunning = false;
                process?.Dispose();
            }
        }

        public void DispatchPendingEvents() {
            // The loop is limited to 1000 iterations as a workaround for an issue that some users
            // have experienced. Sometimes EventsPending starts return 'true' for all iterations,
            // causing the loop to never end.

            int n = 1000;
            Gdk.Threads.Enter();

            while (Gtk.Application.EventsPending() && --n > 0) {
                Gtk.Application.RunIteration(false);
            }

            Gdk.Threads.Leave();
        }

    }

}