using System;
using System.Collections.Generic;
using System.Linq;
using Blossom.Core;
using Blossom.Core.Visual;
using SkiaSharp;
using Blossom.Testing.Components;
using Blossom.Core.Input;

namespace Blossom.Testing.Views
{
    public class KanbanBoardView : View
    {
        public Action? OnSwitchToDashboard;
        public Action? OnSwitchToNeonShowcase;
        public Action? OnSwitchToPaint;

        private readonly List<TaskItem> _tasks = new();
        private ScrollContainer? _todoScroll;
        private ScrollContainer? _progressScroll;
        private ScrollContainer? _doneScroll;

        private readonly string[] _randomTaskTitles = new[]
        {
            "Center a DIV in CSS",
            "Refactor spaghetti code",
            "Explain bug to rubber duck",
            "Drink coffee & write JIT lints",
            "Squash git merge conflicts",
            "Fix off-by-one rendering loop",
            "Upgrade NuGet packages",
            "Write tests that actually pass",
            "Optimize GPU context pipeline",
            "Deploy to production on Friday"
        };

        private readonly string[] _randomTaskDescs = new[]
        {
            "Requires advanced dark arts and flexbox math.",
            "Untangle 1,500 lines of nested switch statements.",
            "Duck looks disappointed but holds the key to the fix.",
            "Converts caffeine directly into hardware-accelerated code.",
            "Hope nobody committed directly to main branch.",
            "Find why the 61st frame causes a NullReferenceException.",
            "Hope you enjoy resolving 47 version conflicts.",
            "Write mock tests to make coverage charts look green.",
            "Ensure SkiaSharp doesn't drop to 13 FPS in Docker.",
            "YOLO deployment. What is the worst that could happen?"
        };

        private readonly string[] _priorities = new[] { "Low", "Medium", "High" };

        public class TaskItem
        {
            public string Id { get; set; } = Guid.NewGuid().ToString().Substring(0, 5);
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public string Column { get; set; } = "todo"; // "todo", "progress", "done"
            public string Priority { get; set; } = "Medium"; // "Low", "Medium", "High"
        }

        public KanbanBoardView() : base("Task Commander")
        {
            BackColor = new SKColor(15, 23, 42); // Slate 900
            
            // Add initial default tasks
            _tasks.Add(new TaskItem 
            { 
                Title = "Optimize Draw Loop", 
                Description = "Refactor transform evaluation pipeline to avoid heap allocations.", 
                Column = "done", 
                Priority = "High" 
            });
            _tasks.Add(new TaskItem 
            { 
                Title = "Implement Scroll Container", 
                Description = "Create ScrollContainer element with mouse scroll wheel mapping.", 
                Column = "done", 
                Priority = "High" 
            });
            _tasks.Add(new TaskItem 
            { 
                Title = "Build Kanban Board App", 
                Description = "Create a fully functional task board view inside the Blossom engine.", 
                Column = "progress", 
                Priority = "Medium" 
            });
            _tasks.Add(new TaskItem 
            { 
                Title = "Add Animation Support", 
                Description = "Create a basic tweening engine for visual translation effects.", 
                Column = "todo", 
                Priority = "Low" 
            });
        }

        public override void Init()
        {
            float sidebarWidth = 260f;

            // --- 1. SIDEBAR ---
            var sidebar = new VisualElement
            {
                Name = "KanbanSidebar",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(30, 41, 59), // Slate 800
                    Border = new BorderStyle { Width = 0, Color = SKColors.Transparent },
                    Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(60), SpreadX = 6, SpreadY = 0 }
                },
                Transform = new Transform(0, 0, sidebarWidth, Height)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left,
                    FixedWidth = true,
                    FixedHeight = false
                }
            };
            AddElement(sidebar);

            // Brand Label
            sidebar.AddChild(new VisualElement
            {
                Name = "KanbanSidebar_Brand",
                Text = "⚡ BLOSSOM OS",
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = new SKColor(56, 189, 248), Size = 26, Weight = 800, Alignment = TextAlign.Center, Padding = 25 }
                },
                Transform = new Transform(0, 0, sidebarWidth, 80)
            });

            // Navigation menu items
            string[] menuItems = { "Overview", "Neon Showcase", "Neon Paint", "Task Board" };
            float menuY = 100f;
            for (int i = 0; i < menuItems.Length; i++)
            {
                var item = menuItems[i];
                var btn = new Button(item, i == 3 ? new SKColor(255, 255, 255, 15) : SKColors.Transparent)
                {
                    Transform = { X = 20, Y = menuY, Width = sidebarWidth - 40, Height = 45 }
                };
                btn.Style.Text.Alignment = TextAlign.Left;
                btn.Style.Text.Padding = 20;
                btn.Style.Border.Roundness = 8;

                int idx = i;
                btn.OnClick = () =>
                {
                    if (idx == 0) OnSwitchToDashboard?.Invoke();
                    else if (idx == 1) OnSwitchToNeonShowcase?.Invoke();
                    else if (idx == 2) OnSwitchToPaint?.Invoke();
                };

                sidebar.AddChild(btn);
                menuY += 55f;
            }

            // Quick stats panel inside sidebar
            var statsPanel = new VisualElement
            {
                Name = "KanbanStats",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(15, 23, 42, 100),
                    Border = new BorderStyle { Roundness = 12, Width = 1, Color = new SKColor(255, 255, 255, 10) }
                },
                Transform = new Transform(20, Height - 160f, sidebarWidth - 40f, 130f)
                {
                    Anchor = Anchor.Bottom | Anchor.Left,
                    FixedWidth = true,
                    FixedHeight = true
                }
            };
            sidebar.AddChild(statsPanel);

            statsPanel.AddChild(new VisualElement
            {
                Name = "StatsTitle",
                Text = "ENGINE STATUS",
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.Gray, Size = 11, Weight = 700, Padding = 15 } },
                Transform = new Transform(0, 0, sidebarWidth - 40f, 30f)
            });

            statsPanel.AddChild(new VisualElement
            {
                Name = "StatsMemoryLabel",
                Text = "Memory footprint: ~100 MB",
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.LightGray, Size = 12, Padding = 15 } },
                Transform = new Transform(0, 30f, sidebarWidth - 40f, 25f)
            });

            statsPanel.AddChild(new VisualElement
            {
                Name = "StatsTargetLabel",
                Text = "Target: Linux Native 64bit",
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.LightGray, Size = 12, Padding = 15 } },
                Transform = new Transform(0, 55f, sidebarWidth - 40f, 25f)
            });

            statsPanel.AddChild(new VisualElement
            {
                Name = "StatsPerformance",
                Text = "Rendering status: STABLE",
                Style = new ElementStyle { Text = new TextStyle { Color = new SKColor(16, 185, 129), Size = 12, Weight = 600, Padding = 15 } },
                Transform = new Transform(0, 80f, sidebarWidth - 40f, 25f)
            });


            // --- 2. MAIN CONTENT AREA ---
            var mainContent = new VisualElement
            {
                Name = "KanbanMain",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(sidebarWidth, 0, Width - sidebarWidth, Height)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right
                }
            };
            AddElement(mainContent);

            // Title block
            var header = new VisualElement
            {
                Name = "KanbanHeader",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(0, 0, Width - sidebarWidth, 80)
                {
                    Anchor = Anchor.Top | Anchor.Left | Anchor.Right,
                    FixedHeight = true
                }
            };
            mainContent.AddChild(header);

            header.AddChild(new VisualElement
            {
                Name = "KanbanTitle",
                Text = "⚡ TASK COMMANDER",
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.White, Size = 24, Weight = 800, Padding = 25 } },
                Transform = new Transform(0, 0, 400, 80)
            });

            // "Add Random Task" glowing button in header
            var addTaskBtn = new NeonButton("+ ADD RANDOM TASK", new SKColor(56, 189, 248), 200f, 40f)
            {
                Name = "Btn_AddRandomTask",
                Transform = { X = Width - sidebarWidth - 230f, Y = 20f, Anchor = Anchor.Top | Anchor.Right }
            };
            addTaskBtn.OnClick = AddRandomTask;
            header.AddChild(addTaskBtn);


            // --- 3. KANBAN BOARD COLUMNS ---
            float colGap = 15f;
            float leftRightMargin = 20f;
            float columnsAreaWidth = Width - sidebarWidth - leftRightMargin * 2f;
            float colWidth = (columnsAreaWidth - colGap * 2f) / 3f;
            float colHeight = Height - 120f;

            // Column A: TO DO
            var colTodo = CreateColumnContainer("Col_Todo_Frame", "BACKLOG / TO DO", new SKColor(244, 63, 94), 
                sidebarWidth + leftRightMargin, 100f, colWidth, colHeight, out _todoScroll);
            
            // Column B: IN PROGRESS
            var colProgress = CreateColumnContainer("Col_Progress_Frame", "IN PROGRESS", new SKColor(56, 189, 248), 
                sidebarWidth + leftRightMargin + colWidth + colGap, 100f, colWidth, colHeight, out _progressScroll);

            // Column C: DONE
            var colDone = CreateColumnContainer("Col_Done_Frame", "COMPLETED", new SKColor(16, 185, 129), 
                sidebarWidth + leftRightMargin + (colWidth + colGap) * 2f, 100f, colWidth, colHeight, out _doneScroll);

            // Rebuild visual boards
            RebuildBoard();
        }

        private VisualElement CreateColumnContainer(string name, string title, SKColor accentColor, float x, float y, float w, float h, out ScrollContainer scrollArea)
        {
            var frame = new VisualElement
            {
                Name = name,
                Style = new ElementStyle
                {
                    BackColor = new SKColor(30, 41, 59, 120), // Translucent Slate 800
                    Border = new BorderStyle { Roundness = 12, Width = 1, Color = new SKColor(255, 255, 255, 10) }
                },
                Transform = new Transform(x, y, w, h)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left
                }
            };
            AddElement(frame);

            // Accent Line
            frame.AddChild(new VisualElement
            {
                Name = name + "_AccentLine",
                Style = new ElementStyle { BackColor = accentColor },
                Transform = new Transform(0, 0, w, 4) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Column Title text
            frame.AddChild(new VisualElement
            {
                Name = name + "_Title",
                Text = title,
                Style = new ElementStyle
                {
                    Text = new TextStyle { Color = SKColors.White, Size = 13, Weight = 700, Padding = 15 }
                },
                Transform = new Transform(0, 4, w, 40) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Vertical Scroll Container for the Task cards
            scrollArea = new ScrollContainer
            {
                Name = name + "_Scroll",
                Style = new ElementStyle { BackColor = SKColors.Transparent },
                Transform = new Transform(0, 44, w, h - 54)
                {
                    Anchor = Anchor.Top | Anchor.Bottom | Anchor.Left | Anchor.Right
                }
            };
            frame.AddChild(scrollArea);

            return frame;
        }

        private void RebuildBoard()
        {
            if (_todoScroll == null || _progressScroll == null || _doneScroll == null) return;

            // 1. Clear previous cards
            ClearColumnScroll(_todoScroll);
            ClearColumnScroll(_progressScroll);
            ClearColumnScroll(_doneScroll);

            // 2. Add current task cards to columns
            PopulateColumn("todo", _todoScroll);
            PopulateColumn("progress", _progressScroll);
            PopulateColumn("done", _doneScroll);
        }

        private void ClearColumnScroll(ScrollContainer scroll)
        {
            var children = scroll.Children;
            for (int i = children.Length - 1; i >= 0; i--)
            {
                var child = children[i];
                child?.Dispose();
            }
            scroll.ScrollY = 0;
            scroll.ScrollX = 0;
        }

        private void PopulateColumn(string colName, ScrollContainer scroll)
        {
            var tasksInCol = _tasks.Where(t => t.Column == colName).ToList();
            
            float cardY = 10f;
            float cardHeight = 95f;
            float gapY = 10f;
            float cardWidth = scroll.Transform.Computed.Width - 20f;

            for (int i = 0; i < tasksInCol.Count; i++)
            {
                var task = tasksInCol[i];
                var card = CreateTaskCard(task, cardWidth, cardHeight);
                card.Transform.X = 10f;
                card.Transform.Y = cardY;
                card.Transform.Width = cardWidth;
                card.Transform.Height = cardHeight;
                card.Transform.Anchor = Anchor.Top | Anchor.Left | Anchor.Right;
                
                scroll.AddChild(card);
                cardY += cardHeight + gapY;
            }

            scroll.MarkChildrenTransformDirty();
            scroll.ScheduleRender();
        }

        private VisualElement CreateTaskCard(TaskItem task, float width, float height)
        {
            var card = new VisualElement
            {
                Name = $"Card_{task.Id}",
                Style = new ElementStyle
                {
                    BackColor = new SKColor(255, 255, 255, 10), // Glassmorphic effect
                    Border = new BorderStyle { Roundness = 10, Width = 1, Color = new SKColor(255, 255, 255, 20) },
                    Shadow = new ShadowStyle { Color = SKColors.Black.WithAlpha(40), SpreadY = 3, OffsetY = 2 }
                }
            };

            // Task Title
            card.AddChild(new VisualElement
            {
                Name = $"Card_{task.Id}_Title",
                Text = task.Title,
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.White, Size = 13, Weight = 700, Padding = 15 } },
                Transform = new Transform(0, 8, width, 20) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Task Description
            card.AddChild(new VisualElement
            {
                Name = $"Card_{task.Id}_Desc",
                Text = task.Description,
                Style = new ElementStyle { Text = new TextStyle { Color = SKColors.LightGray, Size = 10, Padding = 15 } },
                Transform = new Transform(0, 28, width, 30) { Anchor = Anchor.Top | Anchor.Left | Anchor.Right }
            });

            // Priority Badge Color
            SKColor badgeColor = task.Priority switch
            {
                "High" => new SKColor(244, 63, 94, 50),
                "Medium" => new SKColor(251, 191, 36, 50),
                _ => new SKColor(16, 185, 129, 50)
            };
            SKColor badgeText = task.Priority switch
            {
                "High" => new SKColor(244, 63, 94),
                "Medium" => new SKColor(251, 191, 36),
                _ => new SKColor(16, 185, 129)
            };

            // Priority Badge
            var badge = new VisualElement
            {
                Name = $"Card_{task.Id}_Badge",
                Text = task.Priority.ToUpper(),
                Style = new ElementStyle
                {
                    BackColor = badgeColor,
                    Border = new BorderStyle { Roundness = 4 },
                    Text = new TextStyle { Color = badgeText, Size = 8, Weight = 700, Alignment = TextAlign.Center }
                },
                Transform = new Transform(15, 63, 50, 18) { Anchor = Anchor.Bottom | Anchor.Left }
            };
            card.AddChild(badge);

            // Actions - Move Forward Button
            if (task.Column != "done")
            {
                var moveBtn = new Button("➜", new SKColor(79, 70, 229, 120))
                {
                    Name = $"Card_{task.Id}_MoveBtn",
                    Transform = new Transform(width - 32, 63, 20, 18) { Anchor = Anchor.Bottom | Anchor.Right }
                };
                moveBtn.Style.Text.Color = SKColors.White;
                moveBtn.Style.Text.Size = 10;
                moveBtn.Style.Text.Alignment = TextAlign.Center;
                moveBtn.Style.Border.Roundness = 4;
                moveBtn.OnClick = () => MoveTaskForward(task);
                card.AddChild(moveBtn);
            }

            // Actions - Move Backward Button
            if (task.Column != "todo")
            {
                var moveBackBtn = new Button("clear", SKColors.Transparent)
                {
                    Name = $"Card_{task.Id}_BackBtn",
                    Text = "lt", // Use '<' for backward movement
                    Style = new ElementStyle 
                    { 
                        BackColor = new SKColor(71, 85, 105, 120),
                        Border = new BorderStyle { Roundness = 4 },
                        Text = new TextStyle { Color = SKColors.LightGray, Size = 10, Alignment = TextAlign.Center }
                    },
                    Transform = new Transform(width - 57, 63, 20, 18) { Anchor = Anchor.Bottom | Anchor.Right }
                };
                
                // Set literal back arrow character
                moveBackBtn.Text = "lt"; 
                // Wait! To render properly as '<', we should assign it
                moveBackBtn.Text = "<";
                
                moveBackBtn.OnClick = () => MoveTaskBackward(task);
                card.AddChild(moveBackBtn);
            }

            // Actions - Delete Button
            var deleteBtn = new Button("✖", new SKColor(244, 63, 94, 30))
            {
                Name = $"Card_{task.Id}_DeleteBtn",
                Transform = new Transform(width - (task.Column == "done" ? 32 : (task.Column == "todo" ? 57 : 82)), 63, 20, 18) { Anchor = Anchor.Bottom | Anchor.Right }
            };
            deleteBtn.Style.Text.Color = new SKColor(244, 63, 94);
            deleteBtn.Style.Text.Size = 10;
            deleteBtn.Style.Text.Alignment = TextAlign.Center;
            deleteBtn.Style.Border.Roundness = 4;
            deleteBtn.OnClick = () => DeleteTask(task);
            card.AddChild(deleteBtn);

            // Card Hover Effect
            card.Events.OnMouseEnter += (s) =>
            {
                card.Style.BackColor = new SKColor(255, 255, 255, 18);
                card.Style.Border.Color = new SKColor(255, 255, 255, 35);
                card.ScheduleRender();
            };
            card.Events.OnMouseLeave += (s) =>
            {
                card.Style.BackColor = new SKColor(255, 255, 255, 10);
                card.Style.Border.Color = new SKColor(255, 255, 255, 20);
                card.ScheduleRender();
            };

            return card;
        }

        private void AddRandomTask()
        {
            var rand = new Random();
            int titleIdx = rand.Next(_randomTaskTitles.Length);
            int priorityIdx = rand.Next(_priorities.Length);

            var newTask = new TaskItem
            {
                Title = _randomTaskTitles[titleIdx],
                Description = _randomTaskDescs[titleIdx],
                Priority = _priorities[priorityIdx],
                Column = "todo"
            };

            _tasks.Add(newTask);
            RebuildBoard();
        }

        private void MoveTaskForward(TaskItem task)
        {
            if (task.Column == "todo") task.Column = "progress";
            else if (task.Column == "progress") task.Column = "done";
            RebuildBoard();
        }

        private void MoveTaskBackward(TaskItem task)
        {
            if (task.Column == "done") task.Column = "progress";
            else if (task.Column == "progress") task.Column = "todo";
            RebuildBoard();
        }

        private void DeleteTask(TaskItem task)
        {
            _tasks.Remove(task);
            RebuildBoard();
        }
    }
}
