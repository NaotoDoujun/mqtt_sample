import React from 'react'
import useMediaQuery from '@material-ui/core/useMediaQuery'
import { CssBaseline, AppBar, Toolbar, Typography } from '@material-ui/core'
import { createMuiTheme, ThemeProvider, makeStyles } from '@material-ui/core/styles'
import { BrowserRouter, Route, Switch } from 'react-router-dom'
import { Count, Sub } from './components'

const useStyles = makeStyles((theme) => ({
  root: {
    display: 'flex',
  },
  content: {
    flexGrow: 1,
    paddingTop: theme.spacing(8),
    paddingLeft: theme.spacing(2),
    paddingRight: theme.spacing(2),
    paddingBottom: theme.spacing(2),
  },
}));

function App() {
  const classes = useStyles();
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
  return (
    <BrowserRouter>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <div className={classes.root}>
          <AppBar position="fixed">
            <Toolbar>
              <Typography variant="h6">Test</Typography>
            </Toolbar>
          </AppBar>
          <main className={classes.content}>
            <Switch>
              <Route exact path='/' component={Count} />
              <Route path='/sub' component={Sub} />
            </Switch>
          </main>
        </div>
      </ThemeProvider>
    </BrowserRouter>
  );
}

export default App;
