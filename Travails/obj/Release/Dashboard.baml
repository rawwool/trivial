   M S B A M L   `   `   `  ���� D  @Trivial, Version=1.0.100.0, Culture=neutral, PublicKeyToken=null/clr-namespace:Travails.ModelTravails.Model      Travails.Dashboard   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     @d2http://schemas.microsoft.com/expression/blend/2008     +myclr-namespace:Travails.Model     -     >�Name$  	dashboard�� � 	Resources    StoryboardPulsate g� RepeatBehavior �� TargetProperty9  3System.Windows.Media.Animation.EasingDoubleKeyFrame O� KeyTime O� Value  StoryboardPulsate2 g� AutoReverse    Travails.Model.MyConverter  MyConverter  ScrollThumbs �� 
TargetType �� Property	 �� Value
 �� 
TargetType �Name ��Name �� CornerRadius �� Triggers P� Property P� Value �� 
TargetName � RowDefinitions ]�Name ]� Thumb n�Name ]� IncreaseRepeatButton ��Name J� Opacity ]� DecreaseRepeatButton P� 
SourceName  ButtonSelectBrush  	DarkBrush �� Angle  itemsTemplate �� Padding J� InputBindings d� MouseAction �� Command  � ColumnDefinitions! ��Name" �� CommandParameter# `� 	Converter$ �� Path% 
� GradientOrigin& �� RelativeTransform' �� CenterX( �� CenterY) �� ScaleX* �� ScaleY+ T� X, T� Y- � Offset. � Color��%�  &	        &	 ?    &	 �    &	 �    '�� 6    &	 *    7  �� $ 6x��.��U� $9 3(UIElement.RenderTransform).(ScaleTransform.ScaleX)�.\�  $ 0:0:0.1��$	 0.9Q�  $ 0:0:0.2��$	 1.1Q�  $ 0:0:0.3��$ 1Q�U� $9 3(UIElement.RenderTransform).(ScaleTransform.ScaleY)�.\�  $ 0:0:0.1��$	 0.9Q�  $ 0:0:0.2��$	 1.1Q�  $ 0:0:0.3��$ 1Q��� $ 6x��.��U� $9 3(UIElement.RenderTransform).(ScaleTransform.ScaleX)�.\�  $ 0:0:0.1��$	 0.9Q�  $ 0:0:0.2��$	 1.1Q�  $ 0:0:0.3��$ 1Q�U� $9 3(UIElement.RenderTransform).(ScaleTransform.ScaleY)�.\�  $ 0:0:0.1��$	 0.9Q�  $ 0:0:0.2��$	 1.1Q�  $ 0:0:0.3��$ 1Q�U� $ (UIElement.Opacity)� . .\�  $ 0:0:0.3��$ 1Q�  $ 0:0:0.4��$ 0.003Q�  �� " n�.���  � ��	 �� "
 n�.R�� $
 Grid��.+��� $��Stretch=�$��Stretch=�$
��Auto��$
��Auto��
������ �� $ 
Rectangle1��$ 10��$��Stretch=�$��Stretch=�$
��Auto��$
��Auto��#��z�� P�  �@�Tag$ 
Horizontal��. ��� $ 
Rectangle1�� � ��$
	 Auto���� $ 
Rectangle1�� � ��$	 7���� " ��.���  �@��IsFlicksEnabled	 .  ��  � ��
	 �������  � ��$	 7����  � ��	 �� "
 ��.R�� $ GridRoot��$��8��#��z�� �� $��0.00001* �.+�]� $ 
PART_Track��$��0����. |�.   n� $ Thumb��#��z��#���   �� $ PageUp��$��ScrollBar.PageDownCommand��$ 0Q�|�.   �� $ PageDown��$��ScrollBar.PageUpCommand��$ 0Q�|�.   P� $ Thumb�� �@J�IsMouseOver . . ��� #	 �  $ Thumb�� � ��P� $ Thumb�� �@n�
IsDragging . . ��� #	 �  $ Thumb�� � ��P�  � {� .  . ��� $ Thumb�� � y�$	 	Collapsed=�P�  �@��Orientation$ 
Horizontal=�. ��� $ GridRoot�� �@�LayoutTransform	 �� $	 -90Q��� $ 
PART_Track�� �@�LayoutTransform	 �� $	 -90Q���  � ��$
	 Auto����  � ��$	 12���� $ Thumb�� �@�Tag$	 
Horizontal���� $ PageDown�� � ��$	 ScrollBar.PageLeftCommand���� $ PageUp�� � ��$ 	 ScrollBar.PageRightCommand���� .R��� $
��Auto��$��50��$ 5,5,5,5��$��1,0,1,0q�$ 1,1,1,1q� d� $ 	LeftClicke� �� *MyCustomCommand+._�� $
��Auto��$
��Auto��  �� $��20 ��� $��* �.+��� $! myText��$
��Left=�$��2,2,2,2q�$��Center=�$��1��$��12(�$��Verdana)�$
��Bold"�
���   �$
��Wrap=����� *Title+ d� $ 	LeftClicke� �� *MyCustomCommand+" `� 8#   .1��� $$ Title��� $
$ Date�A� $��20��$��20��$��1��$��Center=�$��Center=����� *Fill+A� $��0��$��15��$��15��$	��0.1��$��Center=�$��Center=����	#FF0000FFz�V� .~��� �� �� T� ��
� $% 0.496,1.052&�& V� .~��� $	' 0.5Q�$	( 0.5Q�$	) 1.5Q�$	* 1.5Q�T� $
+ 0.02Q�$	, 0.3Q�.��� $- 1Q�. �� *Color+� $	- 0.4Q�. �� *Color+.���� $ 1Q�.+��� ���� / �� RelativeSource/ �� *FindAncestor+0 �� AncestorType"0 ��$$ ActualHeight�.���� $
��Auto��$
��Auto��#��[ ���� 1 �� ElementName$1 	dashboard��$$ Actions�