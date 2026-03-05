import { Stack } from 'expo-router';
import { useTheme } from '../../../src/context/ThemeContext';
import { BackToMoreButton } from '../../../src/components/BackToMoreButton';

export default function RecurringLayout() {
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
          title: 'Recurring',
          headerLeft: () => <BackToMoreButton />,
        }}
      />
      <Stack.Screen name="create" options={{ title: 'Add recurring' }} />
    </Stack>
  );
}
