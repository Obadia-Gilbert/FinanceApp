import { View, Text } from 'react-native';
import { Stack } from 'expo-router';
import { useTheme } from '../../../src/context/ThemeContext';
import { BackToMoreButton } from '../../../src/components/BackToMoreButton';

export default function CategoriesLayout() {
  const { colors } = useTheme();
  return (
    <Stack
      screenOptions={{
        headerStyle: { backgroundColor: colors.bg.default },
        headerTintColor: colors.text.primary,
        headerShadowVisible: false,
        headerBackTitle: 'Back',
      }}
    >
      <Stack.Screen
        name="index"
        options={{
          title: 'Manage Categories',
          headerLeft: () => <BackToMoreButton />,
          headerRight: () => (
            <View style={{ marginRight: 16 }}>
              <Text style={{ fontSize: 18 }}>🔍</Text>
            </View>
          ),
        }}
      />
      <Stack.Screen name="create" options={{ title: 'New Category' }} />
      <Stack.Screen name="[id]" options={{ title: 'Edit Category' }} />
    </Stack>
  );
}
