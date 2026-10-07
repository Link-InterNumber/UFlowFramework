#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace PowerCellStudio.Editor
{
	[CustomEditor(typeof(Notifier))]
	public sealed class NotifierEditor : UnityEditor.Editor
	{
		private const string TypeNameProperty = "typeName";
		private const string TypeIndexProperty = "notifyTypeIndex";

		private readonly List<Type> _typeCandidates = new List<Type>();
		private SearchField _typeSearchField;
		private string _searchText = string.Empty;
		private Type _selectedEnumType;
		private string _resolvedTypeName;
		private bool _showTypeCandidates;

		private SerializedProperty _typeNameProperty;
		private SerializedProperty _typeIndexProperty;

		private void OnEnable()
		{
			_typeSearchField = new SearchField();
			_typeNameProperty = serializedObject.FindProperty(TypeNameProperty);
			_typeIndexProperty = serializedObject.FindProperty(TypeIndexProperty);
			RefreshSelectedType();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawTypeSelector();
			DrawEnumValueSelector();
			DrawRemainingProperties();

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawTypeSelector()
		{
			EditorGUILayout.LabelField("Notification Enum Type", EditorStyles.boldLabel);

			var searchRect = EditorGUILayout.GetControlRect();
			var newSearchText = _typeSearchField.OnGUI(searchRect, _searchText);
			if (!string.Equals(newSearchText, _searchText, StringComparison.Ordinal))
			{
				_searchText = newSearchText;
				_showTypeCandidates = true;
				RefreshTypeCandidates(_searchText);
			}

			var currentTypeName = _typeNameProperty.stringValue;
			if (!string.Equals(currentTypeName, _resolvedTypeName, StringComparison.Ordinal))
				RefreshSelectedType();

			if (_selectedEnumType != null)
			{
				EditorGUILayout.HelpBox(_selectedEnumType.FullName, MessageType.Info);
			}
			else if (!string.IsNullOrWhiteSpace(currentTypeName))
			{
				EditorGUILayout.HelpBox("The selected type does not exist or is not an enum.", MessageType.Error);
			}

			if (_showTypeCandidates)
				DrawTypeCandidates();
		}

		private void DrawTypeCandidates()
		{
			if (_typeCandidates.Count == 0)
			{
				EditorGUILayout.HelpBox("No matching types found.", MessageType.None);
				return;
			}

			var maxResults = Mathf.Min(_typeCandidates.Count, 12);
			for (var index = 0; index < maxResults; index++)
			{
				var type = _typeCandidates[index];
				var label = type.FullName ?? type.Name;
				var typeLabel = type.IsEnum ? label : label + " (not enum)";
				var buttonContent = new GUIContent(typeLabel);
				if (!GUILayout.Button(buttonContent, EditorStyles.miniButton))
					continue;

				if (!type.IsEnum)
				{
					EditorUtility.DisplayDialog("Invalid notification type",
						$"'{label}' is not an enum type.", "OK");
					continue;
				}

				_typeNameProperty.stringValue = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
				_typeIndexProperty.intValue = 0;
				_selectedEnumType = type;
				_resolvedTypeName = _typeNameProperty.stringValue;
				_searchText = type.FullName ?? type.Name;
				_showTypeCandidates = false;
				GUI.FocusControl(null);
				GUI.changed = true;
				break;
			}
		}

		private void DrawEnumValueSelector()
		{
			if (_selectedEnumType == null)
				return;

			var enumValues = Enum.GetValues(_selectedEnumType);
			if (enumValues.Length == 0)
			{
				EditorGUILayout.HelpBox("The enum does not contain any values.", MessageType.Warning);
				return;
			}

			var currentIndex = Mathf.Clamp(_typeIndexProperty.intValue, 0, enumValues.Length - 1);
			var displayNames = new string[enumValues.Length];
			for (var index = 0; index < enumValues.Length; index++)
				displayNames[index] = $"{index}: {enumValues.GetValue(index)}";

			var selectedIndex = EditorGUILayout.Popup("Notification Value", currentIndex, displayNames);
			if (selectedIndex != currentIndex)
				_typeIndexProperty.intValue = selectedIndex;
		}

		private void DrawRemainingProperties()
		{
			var iterator = serializedObject.GetIterator();
			var enterChildren = true;
			while (iterator.NextVisible(enterChildren))
			{
				enterChildren = false;
				if (iterator.propertyPath == "m_Script" ||
					iterator.propertyPath == TypeNameProperty ||
					iterator.propertyPath == TypeIndexProperty)
					continue;

				EditorGUILayout.PropertyField(iterator, true);
			}
		}

		private void RefreshSelectedType()
		{
			_resolvedTypeName = _typeNameProperty == null ? string.Empty : _typeNameProperty.stringValue;
			_selectedEnumType = null;
			if (string.IsNullOrWhiteSpace(_resolvedTypeName))
				return;

			var resolvedType = ReflectionUtils.GetTypeByName(_resolvedTypeName);
			if (resolvedType != null && resolvedType.IsEnum)
				_selectedEnumType = resolvedType;
		}

		private void RefreshTypeCandidates(string searchText)
		{
			_typeCandidates.Clear();
			if (string.IsNullOrWhiteSpace(searchText))
				return;

			var normalizedSearch = searchText.Trim();
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			var seenTypes = new HashSet<Type>();
			for (var assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
			{
				Type[] types;
				try
				{
					types = assemblies[assemblyIndex].GetTypes();
				}
				catch (ReflectionTypeLoadException exception)
				{
					types = exception.Types;
				}

				if (types == null)
					continue;

				for (var typeIndex = 0; typeIndex < types.Length; typeIndex++)
				{
					var type = types[typeIndex];
					if (type == null || type.IsGenericTypeDefinition || type.IsNestedPrivate ||
						type.FullName == null || !seenTypes.Add(type))
						continue;

					if (FuzzyMatch(type, normalizedSearch))
						_typeCandidates.Add(type);
				}
			}

			_typeCandidates.Sort(CompareTypes);
		}

		private static bool FuzzyMatch(Type type, string searchText)
		{
			var name = type.Name;
			var fullName = type.FullName;
			if (name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
				fullName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
				return true;

			var searchIndex = 0;
			for (var nameIndex = 0; nameIndex < name.Length && searchIndex < searchText.Length; nameIndex++)
			{
				if (char.ToLowerInvariant(name[nameIndex]) == char.ToLowerInvariant(searchText[searchIndex]))
					searchIndex++;
			}
			return searchIndex == searchText.Length;
		}

		private static int CompareTypes(Type left, Type right)
		{
			if (left.IsEnum != right.IsEnum)
				return left.IsEnum ? -1 : 1;
			return string.Compare(left.FullName, right.FullName, StringComparison.OrdinalIgnoreCase);
		}
	}
}

#endif
