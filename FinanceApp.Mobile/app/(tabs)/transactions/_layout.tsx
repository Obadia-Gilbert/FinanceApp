import { View, Text } from 'react-native';
import { Stack } from 'expo-router';
import { useTheme } from '../../../src/context/ThemeContext';
import { BackToMoreButton } from '../../../src/components/BackToMoreButton';

export default function TransactionsLayout() {
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
          title: 'Transactions',
          headerLeft: () => <BackToMoreButton />,
          headerRight: () => (
            <View style={{ marginRight: 16 }}>
              <Text style={{ fontSize: 20 }}>📅</Text>
            </View>
          ),
        }}
      />
      <Stack.Screen name="create" options={{ title: 'New transaction' }} />
      <Stack.Screen name="transfer" options={{ title: 'Transfer' }} />
    </Stack>
  );
}
