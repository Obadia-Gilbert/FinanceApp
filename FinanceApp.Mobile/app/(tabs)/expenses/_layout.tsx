import { Stack } from 'expo-router';
import { useTheme } from '../../../src/context/ThemeContext';
import { HeaderNotificationIcon } from '../../../src/components/HeaderNotificationIcon';

export default function ExpensesLayout() {
  const { colors } = useTheme();
  return (
    <Stack
      screenOptions={{
        headerStyle: { backgroundColor: colors.bg.default },
        headerTintColor: colors.text.primary,
        headerShadowVisible: false,
        headerBackTitle: 'Back',
        headerRight: () => <HeaderNotificationIcon />,
      }}
    >
      <Stack.Screen name="index" options={{ title: 'Expenses' }} />
      <Stack.Screen name="create" options={{ title: 'Add expense' }} />
      <Stack.Screen name="[id]" options={{ title: 'Expense' }} />
    </Stack>
  );
}
