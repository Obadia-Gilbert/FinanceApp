import { View, Text, StyleSheet } from 'react-native';
import { Tabs } from 'expo-router';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '../../src/context/ThemeContext';
import { BackToMoreButton } from '../../src/components/BackToMoreButton';
import { HeaderNotificationIcon } from '../../src/components/HeaderNotificationIcon';
import { HeaderScreenIcon } from '../../src/components/HeaderScreenIcon';

const tabIcons: Record<string, string> = {
  index: '▣',
  expenses: '¢',
  budget: '◉',
  more: '☰',
};

export default function TabsLayout() {
  const { colors } = useTheme();
  const insets = useSafeAreaInsets();
  return (
    <Tabs
      screenOptions={{
        headerStyle: { backgroundColor: colors.bg.default },
        headerTintColor: colors.text.primary,
        headerShadowVisible: false,
        headerRight: () => <HeaderNotificationIcon />,
        tabBarActiveTintColor: colors.brand,
        tabBarInactiveTintColor: colors.text.muted,
        sceneContainerStyle: { paddingTop: insets.top },
        tabBarStyle: {
          backgroundColor: colors.bg.default,
          borderTopColor: colors.border,
          paddingBottom: insets.bottom,
          height: 56 + insets.bottom,
        },
      }}
    >
      <Tabs.Screen
        name="index"
        options={{
          title: 'Dashboard',
          tabBarLabel: 'Dashboard',
          headerShown: false,
          tabBarIcon: ({ color, size }) => <TabIcon char={tabIcons.index} color={color} size={size} />,
        }}
      />
      <Tabs.Screen
        name="expenses"
        options={{
          title: 'Expenses',
          tabBarLabel: 'Expenses',
          headerShown: false,
          tabBarIcon: ({ color, size }) => <TabIcon char={tabIcons.expenses} color={color} size={size} />,
        }}
      />
      <Tabs.Screen
        name="budget"
        options={{
          title: 'Budget',
          tabBarLabel: 'Budget',
          headerShown: true,
          headerLeft: () => <HeaderScreenIcon char="◉" />,
          headerRight: () => <HeaderNotificationIcon />,
          tabBarIcon: ({ color, size }) => <TabIcon char={tabIcons.budget} color={color} size={size} />,
        }}
      />
      <Tabs.Screen
        name="more"
        options={{
          title: 'More',
          tabBarLabel: 'More',
          headerShown: true,
          headerLeft: () => (
            <View style={styles.headerLeftIcon}>
              <Text style={[styles.headerIconText, { color: colors.text.primary }]}>{tabIcons.more}</Text>
            </View>
          ),
          headerRight: () => <HeaderNotificationIcon />,
          tabBarIcon: ({ color, size }) => <TabIcon char={tabIcons.more} color={color} size={size} />,
        }}
      />
      <Tabs.Screen
        name="profile"
        options={{ href: null, title: 'Profile', headerLeft: () => <BackToMoreButton /> }}
      />
      <Tabs.Screen name="categories" options={{ href: null, headerShown: false }} />
      <Tabs.Screen
        name="privacy"
        options={{ href: null, title: 'Privacy Policy', headerLeft: () => <BackToMoreButton /> }}
      />
      <Tabs.Screen name="income" options={{ href: null, headerShown: false }} />
      <Tabs.Screen name="accounts" options={{ href: null, headerShown: false }} />
      <Tabs.Screen name="transactions" options={{ href: null, headerShown: false }} />
      <Tabs.Screen
        name="notifications"
        options={{ href: null, title: 'Notifications', headerLeft: () => <BackToMoreButton /> }}
      />
      <Tabs.Screen name="recurring" options={{ href: null, headerShown: false }} />
      <Tabs.Screen
        name="feedback"
        options={{ href: null, title: 'Feedback', headerLeft: () => <BackToMoreButton /> }}
      />
      <Tabs.Screen
        name="reports"
        options={{ href: null, title: 'Monthly Report', headerLeft: () => <BackToMoreButton /> }}
      />
      <Tabs.Screen
        name="subscription"
        options={{ href: null, title: 'Subscription Plans', headerLeft: () => <BackToMoreButton /> }}
      />
    </Tabs>
  );
}

const styles = StyleSheet.create({
  headerLeftIcon: { marginLeft: 8, padding: 8, justifyContent: 'center', alignItems: 'center' },
  headerIconText: { fontSize: 22 },
});

function TabIcon({ char, color, size }: { char: string; color: string; size: number }) {
  return (
    <View style={{ width: size, height: size, justifyContent: 'center', alignItems: 'center' }}>
      <Text style={{ color, fontSize: size * 0.85 }}>{char}</Text>
    </View>
  );
}
