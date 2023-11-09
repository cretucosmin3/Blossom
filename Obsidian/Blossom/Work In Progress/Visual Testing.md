
---
#### Create class for creating tests within applications / views

##### Register a test
A test application can have multiple views
A view can have multiple test methods with custom attributes
Each method would be called in a sequence

Example:
``` C#
    public class AnchorsViewTest : AnchorsView, ViewTest
    {
        [VisualTest("Click on the draggable")]
        public void ClickDraggable()
        {
	        
        }
    }
```

Execute a test
