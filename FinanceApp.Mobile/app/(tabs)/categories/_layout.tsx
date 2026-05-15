import { View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { Stack } from 'expo-router';
import { useTheme } from '../../../src/context/ThemeContext';
import { BackToMoreButton } from '../../../src/components/BackToMoreButton';

/** Decorative header affordance aligned with in-page search (theme-safe; no emoji color issues). */
function HeaderSearchGlyph() {
  const { colors } = useTheme();
  return (
    <View
      style={{ marginRight: 8, paddingVertical: 6, opacity: 0.85 }}
      pointerEvents="none"
      accessible
      accessibilityLabel="Search categories"
      accessibilityRole="image"
    >
      <Ionicons name="search-outline" size={24} color={colors.text.muted} />
    </View>
  );
}

export default function CategoriesLayout() {
  const { colors } = useTheme();
  return (
    <Stack
      screenOptions={{
        headerStyle: { backgroundColor: colors.bg.default },
        headerTintColor: colors.text.primary,
        headerTitleStyle: { color: colors.text.primary, fontWeight: '600' },
        headerShadowVisible: false,
        headerBackTitle: 'Back',
      }}
    >
      <Stack.Screen
        name="index"
        options={{
          title: 'Manage Categories',
          headerLeft: () => <BackToMoreButton />,
          headerRight: () => <HeaderSearchGlyph />,
        }}
      />
      <Stack.Screen name="create" options={{ title: 'New Category' }} />
      <Stack.Screen name="[id]" options={{ title: 'Edit Category' }} />
    </Stack>
  );
}
