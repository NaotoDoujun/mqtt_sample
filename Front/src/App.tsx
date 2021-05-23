import React from 'react'
import clsx from 'clsx'
import useMediaQuery from '@material-ui/core/useMediaQuery'
import {
  CssBaseline,
  AppBar,
  Toolbar,
  Typography,
  IconButton,
  Drawer,
  Divider,
  List,
  ListItem,
  ListItemIcon,
  ListItemText
} from '@material-ui/core'
import { createMuiTheme, ThemeProvider, makeStyles } from '@material-ui/core/styles'
import { Menu, ChevronLeft, ChevronRight, Dashboard, ListAlt, Timeline, Settings } from '@material-ui/icons'
import { BrowserRouter, Route, Switch, Link } from 'react-router-dom'
import {
  Dashboard as DashboardComponent,
  Detail as DetailComponent,
  List as ListComponent,
  Graph as GraphComponent,
  Settings as SettingsComponent
} from './components'

const drawerWidth = 240;
const useStyles = makeStyles((theme) => ({
  root: {
    display: 'flex',
  },
  appBar: {
    zIndex: theme.zIndex.drawer + 1,
    transition: theme.transitions.create(['width', 'margin'], {
      easing: theme.transitions.easing.sharp,
      duration: theme.transitions.duration.leavingScreen,
    }),
  },
  appBarShift: {
    marginLeft: drawerWidth,
    width: `calc(100% - ${drawerWidth}px)`,
    transition: theme.transitions.create(['width', 'margin'], {
      easing: theme.transitions.easing.sharp,
      duration: theme.transitions.duration.enteringScreen,
    }),
  },
  grow: {
    flexGrow: 1,
  },
  menuButton: {
    marginRight: 24,
  },
  hide: {
    display: 'none',
  },
  drawer: {
    width: drawerWidth,
    flexShrink: 0,
    whiteSpace: 'nowrap',
  },
  drawerOpen: {
    width: drawerWidth,
    transition: theme.transitions.create('width', {
      easing: theme.transitions.easing.sharp,
      duration: theme.transitions.duration.enteringScreen,
    }),
  },
  drawerClose: {
    transition: theme.transitions.create('width', {
      easing: theme.transitions.easing.sharp,
      duration: theme.transitions.duration.leavingScreen,
    }),
    overflowX: 'hidden',
    width: theme.spacing(7) + 1,
    [theme.breakpoints.up('sm')]: {
      width: theme.spacing(9) + 1,
    },
  },
  toolbar: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'flex-end',
    padding: theme.spacing(0, 1),
    // necessary for content to be below app bar
    ...theme.mixins.toolbar,
  },
  content: {
    flexGrow: 1,
    padding: theme.spacing(3),
  },
}));

function App() {
  const classes = useStyles()
  const [open, setOpen] = React.useState(true)
  const prefersDarkMode = useMediaQuery('(prefers-color-scheme: dark)')
  const theme = React.useMemo(
    () =>
      createMuiTheme({
        props: {
          MuiTextField: {
            variant: "outlined"
          }
        },
        typography: {
          button: {
            textTransform: "none"
          }
        },
        palette: {
          type: prefersDarkMode ? 'dark' : 'light',
        },
      }),
    [prefersDarkMode],
  )

  const handleDrawerOpen = () => {
    setOpen(true)
  }

  const handleDrawerClose = () => {
    setOpen(false)
  }

  return (
    <BrowserRouter>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <div className={classes.root}>
          <AppBar position="fixed" className={clsx(classes.appBar, {
            [classes.appBarShift]: open,
          })}>
            <Toolbar>
              <IconButton
                color="inherit"
                aria-label="open drawer"
                onClick={handleDrawerOpen}
                edge="start"
                className={clsx(classes.menuButton, {
                  [classes.hide]: open,
                })}
              ><Menu /></IconButton>
              <Typography variant="h6">Sample App</Typography>
              <div className={classes.grow} />
              <IconButton
                color="inherit"
                aria-label="settings"
                component={Link} to="/settings"><Settings /></IconButton>
            </Toolbar>
          </AppBar>
          <Drawer
            variant="permanent"
            className={clsx(classes.drawer, {
              [classes.drawerOpen]: open,
              [classes.drawerClose]: !open,
            })}
            classes={{
              paper: clsx({
                [classes.drawerOpen]: open,
                [classes.drawerClose]: !open,
              }),
            }}
          >
            <div className={classes.toolbar}>
              <IconButton onClick={handleDrawerClose}>
                {theme.direction === 'rtl' ? <ChevronRight /> : <ChevronLeft />}
              </IconButton>
            </div>
            <Divider />
            <List>
              <ListItem button key='Dashboard' component={Link} to="/">
                <ListItemIcon><Dashboard /></ListItemIcon>
                <ListItemText primary='Dashboard' />
              </ListItem>
              <ListItem button key='List' component={Link} to="/list">
                <ListItemIcon><ListAlt /></ListItemIcon>
                <ListItemText primary='List' />
              </ListItem>
              <ListItem button key='Graph' component={Link} to="/graph">
                <ListItemIcon><Timeline /></ListItemIcon>
                <ListItemText primary='Graph' />
              </ListItem>
            </List>
          </Drawer>
          <main className={classes.content}>
            <div className={classes.toolbar} />
            <Switch>
              <Route exact path='/' component={DashboardComponent} />
              <Route exact path='/detail/:id' component={DetailComponent} />
              <Route exact path='/list' component={ListComponent} />
              <Route exact path='/graph' component={GraphComponent} />
              <Route exact path='/settings' component={SettingsComponent} />
              <Route component={DashboardComponent} />
            </Switch>
          </main>
        </div>
      </ThemeProvider>
    </BrowserRouter>
  );
}

export default App;
