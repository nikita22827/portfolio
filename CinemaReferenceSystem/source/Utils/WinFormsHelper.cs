using System.ComponentModel;
using System.Reflection;

namespace CinemaReferenceSystem.Utils;

public static class WinFormsHelper
{
    public static readonly Dictionary<string, string> HeaderMap = new Dictionary<string, string>
    {
        ["Id"] = "ID",
        ["Username"] = "Логин",
        ["Role"] = "Роль",
        ["CreatedAt"] = "Создан",
        ["PasswordHash"] = "Хэш пароля",
        ["Name"] = "Название кинотеатра",
        ["Address"] = "Адрес",
        ["Phone"] = "Телефон",
        ["CinemaId"] = "ID кинотеатра",
        ["HallNumber"] = "Номер зала",
        ["RowsCount"] = "Рядов",
        ["MaxSeatsPerRow"] = "Макс. мест в ряду",
        ["Title"] = "Название",
        ["Genre"] = "Жанр",
        ["DurationMinutes"] = "Длительность (мин)",
        ["Director"] = "Режиссёр",
        ["PosterUrl"] = "Ссылка на постер",
        ["TrailerUrl"] = "Трейлер",
        ["Country"] = "Страна",
        ["PremiereDate"] = "Дата премьеры",
        ["ReleaseYear"] = "Год выпуска",
        ["HallId"] = "ID зала",
        ["MovieId"] = "ID фильма",
        ["StartTime"] = "Дата и время",
        ["Price"] = "Цена",
        ["SessionId"] = "ID сеанса",
        ["RowNum"] = "Ряд",
        ["SeatNum"] = "Место (реал.)",
        ["DisplaySeatNum"] = "Место",
        ["IsSold"] = "Продан",
        ["CinemaName"] = "Кинотеатр",
        ["MovieTitle"] = "Фильм",
        ["City"] = "Город",
        ["Description"] = "Описание",
    };
    public static void BindToDataGrid<T>(DataGridView grid, IEnumerable<T> items)
    {
        grid.DataSource = null;
        var list = items.ToList();
        var bindingSource = new BindingSource { DataSource = list };
        grid.DataSource = bindingSource;

        foreach (DataGridViewColumn col in grid.Columns)
        {
            if (HeaderMap.TryGetValue(col.HeaderText, out var russianHeader))
                col.HeaderText = russianHeader;
        }

        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        if (grid.Columns["PasswordHash"] != null)
            grid.Columns["PasswordHash"].Visible = false;
        if (grid.Columns["SessionId"] != null)
            grid.Columns["SessionId"].Visible = false;
        if (grid.Columns["SeatNum"] != null)
            grid.Columns["SeatNum"].Visible = false;
    }

    public static T? ShowEditForm<T>(T? entity = default, string title = "Редактирование",
    Dictionary<string, List<KeyValuePair<object, string>>>? comboData = null,
    List<string>? excludeProperties = null) where T : class, new()
    {
        var form = new Form
        {
            Text = title,
            Width = 450,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            AutoSize = false
        };

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(10),
            AutoScroll = true
        };

        var controls = new Dictionary<string, Control>();

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.Name != "Id" && p.Name != "PasswordHash")
            .Where(p => excludeProperties == null || !excludeProperties.Contains(p.Name));

        foreach (var prop in properties)
        {
            var label = new Label
            {
                Text = (HeaderMap.TryGetValue(prop.Name, out var russian) ? russian : prop.Name) + ":",
                Width = 110,
                TextAlign = ContentAlignment.MiddleRight
            };
            Control input;

            if (comboData != null && comboData.TryGetValue(prop.Name, out var items))
            {
                var combo = new ComboBox
                {
                    Width = 260,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DisplayMember = "Value",
                    ValueMember = "Key"
                };
                combo.DataSource = new BindingList<KeyValuePair<object, string>>(items);
                input = combo;
            }
            else if (prop.PropertyType == typeof(bool))
            {
                input = new CheckBox();
            }
            else if (prop.PropertyType == typeof(DateTime))
            {
                input = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Custom,
                    CustomFormat = "dd.MM.yyyy HH:mm",
                    ShowUpDown = true,
                    Width = 260
                };
            }
            else if (prop.PropertyType == typeof(decimal))
            {
                input = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 100000,
                    DecimalPlaces = 2,
                    Increment = 10,
                    Width = 260
                };
            }
            else
            {
                if (prop.Name.IndexOf("description", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var txt = new TextBox
                    {
                        Width = 260,
                        Height = 70,
                        Multiline = true,
                        ScrollBars = ScrollBars.Vertical,
                        AcceptsReturn = true
                    };
                    input = txt;
                }
                else
                {
                    var txt = new TextBox
                    {
                        Width = 260
                    };
                    input = txt;
                }
            }

            var row = new FlowLayoutPanel
            {
                Width = 400,
                Height = input.Height + 8,
                WrapContents = false
            };
            row.Controls.Add(label);
            row.Controls.Add(input);
            panel.Controls.Add(row);
            controls[prop.Name] = input;

            if (entity != null)
            {
                var value = prop.GetValue(entity);
                if (input is ComboBox combo && value != null)
                {
                    for (int i = 0; i < combo.Items.Count; i++)
                    {
                        var item = (KeyValuePair<object, string>)combo.Items[i];
                        if (item.Key.Equals(value))
                        {
                            combo.SelectedIndex = i;
                            break;
                        }
                    }
                }
                else if (input is TextBox tb)
                {
                    tb.Text = value?.ToString() ?? "";
                }
                else if (input is CheckBox cb)
                {
                    cb.Checked = value is bool b && b;
                }
                else if (input is DateTimePicker dtp && value is DateTime dt)
                {
                    dtp.Value = dt;
                }
                else if (input is NumericUpDown nud && value is decimal d)
                {
                    nud.Value = d;
                }
            }
        }

        var btnSave = new Button { Text = "Сохранить", Width = 120, Height = 35, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Отмена", Width = 120, Height = 35, DialogResult = DialogResult.Cancel };

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.LeftToRight
        };

        int totalButtonsWidth = btnSave.Width + btnSave.Margin.Left + btnSave.Margin.Right +
                                btnCancel.Width + btnCancel.Margin.Left + btnCancel.Margin.Right;

        int leftPadding = (form.ClientSize.Width - totalButtonsWidth) / 2;

        btnPanel.Padding = new Padding(leftPadding, 5, 0, 0);

        btnPanel.Controls.Add(btnSave);
        btnPanel.Controls.Add(btnCancel);

        form.Controls.Add(panel);
        form.Controls.Add(btnPanel);

        int contentHeight = panel.Padding.Top + panel.Padding.Bottom;
        foreach (Control ctrl in panel.Controls)
        {
            contentHeight += ctrl.Height + ctrl.Margin.Top + ctrl.Margin.Bottom;
        }

        form.ClientSize = new Size(form.ClientSize.Width, contentHeight + btnPanel.Height);

        int maxHeight = SystemInformation.VirtualScreen.Height - 100;
        if (form.Height > maxHeight)
        {
            form.Height = maxHeight;
        }

        if (form.ShowDialog() == DialogResult.OK)
        {
            var result = entity ?? new T();
            foreach (var prop in properties)
            {
                if (controls.TryGetValue(prop.Name, out var control))
                {
                    object? value = null;
                    if (control is ComboBox combo)
                    {
                        var selectedItem = (KeyValuePair<object, string>)combo.SelectedItem;
                        value = selectedItem.Key;
                    }
                    else if (control is TextBox tb)
                    {
                        value = tb.Text;
                    }
                    else if (control is CheckBox cb)
                    {
                        value = cb.Checked;
                    }
                    else if (control is DateTimePicker dtp)
                    {
                        value = dtp.Value;
                    }
                    else if (control is NumericUpDown nud)
                    {
                        value = nud.Value;
                    }

                    if (value != null)
                    {
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        prop.SetValue(result, Convert.ChangeType(value, targetType));
                    }
                }
            }
            return result;
        }
        return null;
    }
}