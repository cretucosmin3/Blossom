using System.Diagnostics;
using System.Text;
using System.Numerics;
using System;
using Blossom.Core;
using Blossom.Core.Visual;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Blossom.Testing
{
    public class PrettyUi : View
    {
        VisualElement SearchBar;
        StringBuilder SearchText = new StringBuilder("");
        private int clickedTimes = 0;
        public PrettyUi() : base("PrettyUi View")
        {
            this.Events.OnKeyType += (ch) =>
            {
                SearchText.Append(ch);
                SearchBar.Text = SearchText.ToString();
                SearchBar.Style.ScheduleRender();
            };

            this.Events.OnKeyDown += (key) =>
            {
                if (SearchText.Length > 0)
                {
                    if (key == 14) SearchText.Remove(SearchText.Length - 1, 1);

                    SearchBar.Text = SearchText.ToString();
                }
            };
        }

        public override void Main()
        {
            var HalfWidth = 1100 / 2;

            SearchBar = new VisualElement()
            {
                Name = "ClickMe",
                Transform = new(HalfWidth - 225, 35, 450, 55)
                {
                    Anchor = Anchor.Top,
                    FixedWidth = true,
                    FixedHeight = true,
                    ValidateOnAnchor = true,
                },
                Style = new()
                {
                    BackColor = new(255, 255, 255, 255),
                    Border = new()
                    {
                        Roundness = 4,
                        Width = 0.5f,
                        Color = new(0, 0, 0, 25)
                    },
                    Text = new()
                    {
                        Size = 26,
                        Spacing = 20,
                        Padding = 20,
                        Weight = 450,
                        Alignment = TextAlign.Left,
                        Color = new(50, 50, 200, 220),
                        Shadow = new()
                        {
                            Color = new(50, 50, 200, 35),
                            SpreadX = 1,
                            SpreadY = 5,
                            OffsetY = 18
                        }
                    },
                    Shadow = new()
                    {
                        Color = new(0, 0, 0, 35),
                        SpreadX = 4,
                        SpreadY = 4,
                        OffsetY = 3
                    }
                },
                Text = "Search ..."
            };

            Action Hovered = () =>
            {
                SearchBar.Style.Border.Width = 1f;
                SearchBar.Style.Border.Color = new(0, 0, 0, 225);
                SearchBar.Style.Text.Color = new(0, 0, 0, 255);

                var animator = new Animator<VisualElement>();
                animator.Animate(SearchBar, x => x.Style.Text.Size, 55, 1000);
            };

            Action ToNormal = () =>
            {
                SearchBar.Style.Border.Width = 0.5f;
                SearchBar.Style.Border.Color = new(0, 0, 0, 25);
                SearchBar.Style.Text.Color = new(0, 0, 0, 180);

                var animator = new Animator<VisualElement>();
                animator.Animate(SearchBar, x => x.Style.Text.Size, 26, 1000);
            };

            SearchBar.Events.OnMouseLeave += (VisualElement e) => ToNormal();
            SearchBar.Events.OnMouseEnter += (VisualElement e) => Hovered();
            SearchBar.Events.OnMouseUp += (int btn, Vector2 pos) => Hovered();
            SearchBar.Events.OnMouseDown += (int btn, Vector2 pos) =>
            {
                SearchBar.Style.Border.Width = 3.5f;
                SearchBar.Style.Border.Color = new(50, 50, 200, 220);
                SearchBar.Style.Text.Color = new(50, 50, 200, 220);
            };


            AddElement(SearchBar);
        }
    }
}

public class Animator<T>
{
    public void Animate(T target, Expression<Func<T, object>> propertyLambda, double targetValue, double duration)
    {
        if (propertyLambda == null)
        {
            return;
        }

        var propPath = GetPropertyPath(propertyLambda);

        var thread = new Thread(() =>
        {
            Stopwatch taskTracker = new Stopwatch();

            MemberExpression memberExpression;
            if (propertyLambda.Body is UnaryExpression unaryExpression)
            {
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else
            {
                memberExpression = (MemberExpression)propertyLambda.Body;
            }

            var propertyInfo = (PropertyInfo)memberExpression.Member;
            // var firstValue = propertyInfo.GetValue(target);

            // var initialValue = Convert.ToDouble(propertyInfo.GetValue(target));
            var difference = 50;

            taskTracker.Start();

            while (taskTracker.ElapsedMilliseconds <= duration)
            {
                var elapsed = taskTracker.Elapsed.TotalMilliseconds / duration;
                var newValue = 26 + (difference * elapsed);
                lock (target)
                {
                    propertyInfo.SetValue(target, newValue);
                }
                Thread.Sleep(2);
            }

            taskTracker.Stop();
        });

        thread.Start();
    }

    public void SetPropertyValue(T obj, Expression<Func<T, object>> propertyLambda, object value)
    {
        // Get the full property path from the lambda expression
        var propertyPath = GetPropertyPath(propertyLambda);

        // Set the value of the property
        SetValue(obj, propertyPath, value);
    }

    // This method extracts the property path from the lambda expression
    public string GetPropertyPath(Expression<Func<T, object>> propertyLambda)
    {
        // First, check if the propertyLambda expression is a simple property access (e.g. "x => x.SomeProperty")
        var property = propertyLambda.Body as MemberExpression;
        if (property != null)
        {
            // If the property expression is a simple property access, we can simply return the property name
            return property.Member.Name;
        }

        // If the propertyLambda expression is more complex (e.g. "x => x.SomeProperty.AnotherProperty"),
        // we need to recursively evaluate the property path by calling GetPropertyPath on the inner expression
        var convert = propertyLambda.Body as UnaryExpression;
        if (convert != null && convert.Operand is MemberExpression)
        {
            var innerProperty = convert.Operand as MemberExpression;
            return GetPropertyPath(Expression.Lambda<Func<T, object>>(innerProperty, propertyLambda.Parameters));
        }

        // If the propertyLambda expression is neither a simple property access nor a complex expression,
        // then it is not a valid property expression and an exception should be thrown
        throw new ArgumentException("Invalid property expression.", nameof(propertyLambda));
    }

    // This method sets the value of the property at the specified path
    public void SetValue(object obj, string propertyPath, object value)
    {
        // Split the property path into individual property names
        string[] propertyNames = propertyPath.Split('.');

        // Iterate over the property names
        object currentObject = obj;
        Type currentType = obj.GetType();
        foreach (string propertyName in propertyNames)
        {
            // Get the property by name
            PropertyInfo property = currentType.GetProperty(propertyName);
            if (property == null)
            {
                throw new ArgumentException($"Property '{propertyName}' not found on type '{currentType.Name}'");
            }

            // If this is the last property in the path, set the value
            if (propertyName == propertyNames[propertyNames.Length])
            {
                property.SetValue(currentObject, value);
                break;
            }

            // Otherwise, get the value of this property and move to the next property
            currentObject = property.GetValue(currentObject);
            currentType = currentObject.GetType();
        }
    }

    public object GetValue(object obj, string propertyPath)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        if (string.IsNullOrEmpty(propertyPath))
        {
            throw new ArgumentException("propertyPath cannot be null or empty", nameof(propertyPath));
        }

        var properties = propertyPath.Split('.');

        foreach (var property in properties)
        {
            obj = obj.GetType().GetProperty(property)?.GetValue(obj);
            if (obj == null)
            {
                break;
            }
        }

        return obj;
    }
}