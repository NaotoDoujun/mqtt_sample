import React from 'react'
import { Card, CardHeader } from '@material-ui/core';
import { Theme, makeStyles, createStyles } from '@material-ui/core/styles'
const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      padding: theme.spacing(1)
    },
    cardheader: {
      backgroundColor: theme.palette.primary.main,
      color: theme.palette.primary.contrastText
    },
  }))
const Settings: React.FC<any> = (props: any) => {
  const classes = useStyles()
  return (
    <Card className={classes.root}>
      <CardHeader title="Settings" className={classes.cardheader} />
    </Card>
  )
}

export { Settings }